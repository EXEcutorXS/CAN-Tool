"""
Sync omnidata.json → Google Sheets (single source of truth).

Usage:
    pip install gspread google-auth
    python sync_to_sheets.py

Auth: create a Google Service Account, download credentials.json,
share the spreadsheet with the service account email.
OR: run with --local to just export CSV files without Sheets access.
"""

import json
import csv
import os
import sys
import argparse

SPREADSHEET_ID = "1yp82L-20k24S9WDrRaPwWgrnC5aBRGzcTNKi3B-124w"
OMNIDATA_PATH  = os.path.join(os.path.dirname(__file__), "..", "CAN Tool", "Resources", "omnidata.json")
CREDS_PATH     = os.path.join(os.path.dirname(__file__), "credentials.json")
EXPORT_DIR     = os.path.join(os.path.dirname(__file__), "sheets_export")

# ── Helpers ────────────────────────────────────────────────────────────────────

def meanings_to_str(val):
    """Serialize meanings dict or preset name to a compact string."""
    if val is None:
        return ""
    if isinstance(val, str):
        return val
    # dict: "0=t_off|1=t_on"
    return "|".join(f"{k}={v}" for k, v in val.items())


def load_omnidata(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


# ── Sheet builders ─────────────────────────────────────────────────────────────

PGN_HEADERS = [
    "id", "name", "multiPack",
    # codegen metadata (fill manually)
    "manual", "notes",
]

PARAM_HEADERS = [
    # ── from omnidata.json ──────────────────────────────────────────────────
    "pgn", "packNumber", "name",
    "startByte", "startBit", "bitLength", "signed",
    "a", "b", "unitType",
    "meanings", "getMeaning", "decoder",
    "answerOnly",
    "var",          # existing link: firmware variable ID
    # ── HCU2 codegen columns (fill manually) ───────────────────────────────
    "varName",      # C #define name, e.g. VAR_VOLTAGE
    "cVar",         # C variable path, e.g. Heaters.Instances[SA].Tliquid
    "cType",        # C type: uint8_t / int8_t / float / uint16_t / uint32_t
    "nodata",       # sentinel value meaning "no data" (e.g. 255, 65535)
    "condition",    # extra C condition string (e.g. "targeted", "D[1]<13&&D[1]>0")
    "postHook",     # C statement to call after assignments (e.g. Monitor.WriteRTC(...))
]

MEANING_PRESET_HEADERS = ["presetName", "value", "label"]


def build_pgn_rows(data):
    rows = [PGN_HEADERS]
    for p in data["pgns"]:
        rows.append([
            p["id"],
            p.get("name", ""),
            "TRUE" if p.get("multiPack") else "FALSE",
            "",   # manual
            "",   # notes
        ])
    return rows


def build_param_rows(data):
    rows = [PARAM_HEADERS]
    for p in data["parameters"]:
        rows.append([
            p.get("pgn",        ""),
            p.get("packNumber", ""),
            p.get("name",       ""),
            p.get("startByte",  ""),
            p.get("startBit",   0),
            p.get("bitLength",  ""),
            "TRUE" if p.get("signed") else "",
            p.get("a",          ""),
            p.get("b",          ""),
            p.get("unitType",   ""),
            meanings_to_str(p.get("meanings")),
            p.get("getMeaning", ""),
            p.get("decoder",    ""),
            "TRUE" if p.get("answerOnly") else "",
            p.get("var",        ""),
            # codegen columns — empty, filled by developer
            "", "", "", "", "", "",
        ])
    return rows


def build_meaning_preset_rows(data):
    rows = [MEANING_PRESET_HEADERS]
    for preset_name, mapping in data.get("meaningPresets", {}).items():
        for value, label in mapping.items():
            rows.append([preset_name, value, label])
    return rows


# ── Export to local CSV ────────────────────────────────────────────────────────

def export_csv(rows, filename):
    os.makedirs(EXPORT_DIR, exist_ok=True)
    path = os.path.join(EXPORT_DIR, filename)
    with open(path, "w", newline="", encoding="utf-8") as f:
        csv.writer(f).writerows(rows)
    print(f"  Wrote {len(rows)-1} rows → {path}")


# ── Push to Google Sheets ──────────────────────────────────────────────────────

def push_to_sheets(pgn_rows, param_rows, preset_rows):
    try:
        import gspread
        from google.oauth2.service_account import Credentials
    except ImportError:
        print("ERROR: run  pip install gspread google-auth  first.")
        sys.exit(1)

    scopes = [
        "https://spreadsheets.google.com/feeds",
        "https://www.googleapis.com/auth/drive",
    ]
    creds  = Credentials.from_service_account_file(CREDS_PATH, scopes=scopes)
    gc     = gspread.authorize(creds)
    sh     = gc.open_by_key(SPREADSHEET_ID)

    def upsert_sheet(title, rows):
        try:
            ws = sh.worksheet(title)
            ws.clear()
        except gspread.WorksheetNotFound:
            ws = sh.add_worksheet(title=title, rows=max(len(rows)+10, 50), cols=len(rows[0])+2)
        ws.update(rows, value_input_option="USER_ENTERED")
        print(f"  Sheet '{title}': {len(rows)-1} rows pushed.")

    upsert_sheet("pgns",            pgn_rows)
    upsert_sheet("parameters",      param_rows)
    upsert_sheet("meaning_presets", preset_rows)


# ── Main ───────────────────────────────────────────────────────────────────────

def main():
    parser = argparse.ArgumentParser(description="Sync omnidata.json to Google Sheets")
    parser.add_argument("--local", action="store_true",
                        help="Export CSV only, do not push to Google Sheets")
    args = parser.parse_args()

    print(f"Loading {OMNIDATA_PATH} ...")
    data = load_omnidata(OMNIDATA_PATH)

    pgn_rows    = build_pgn_rows(data)
    param_rows  = build_param_rows(data)
    preset_rows = build_meaning_preset_rows(data)

    print(f"  PGNs:       {len(pgn_rows)-1}")
    print(f"  Parameters: {len(param_rows)-1}")
    print(f"  Presets:    {len(preset_rows)-1}")

    print("\nExporting CSV ...")
    export_csv(pgn_rows,    "pgns.csv")
    export_csv(param_rows,  "parameters.csv")
    export_csv(preset_rows, "meaning_presets.csv")

    if not args.local:
        if not os.path.exists(CREDS_PATH):
            print(f"\nERROR: {CREDS_PATH} not found.")
            print("To push to Google Sheets:")
            print("  1. Create a Service Account in Google Cloud Console")
            print("  2. Download JSON key → tools/credentials.json")
            print("  3. Share the spreadsheet with the service account email")
            print("\nFor now, import the CSV files manually:")
            print("  Google Sheets → File → Import → Upload → sheets_export/*.csv")
            return
        print("\nPushing to Google Sheets ...")
        push_to_sheets(pgn_rows, param_rows, preset_rows)
        print(f"\nDone! https://docs.google.com/spreadsheets/d/{SPREADSHEET_ID}")
    else:
        print(f"\nDone! Import files from {EXPORT_DIR}")


if __name__ == "__main__":
    main()
