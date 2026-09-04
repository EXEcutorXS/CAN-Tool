using OmniProtocol;
using System;
using System.Linq;
using System.Net;
using System.Text;
using static CAN_Tool.Libs.Helper;

namespace CAN_Tool.Libs
{
    // Собирает отчёт по устройству в виде HTML (с полноценным CSS - скруглённые углы, тени,
    // пастельная палитра), который затем печатается в PDF через HtmlToPdfConverter.
    public static class ReportHtmlBuilder
    {
        private const string AccentColor = "#5B7680";
        private const string HeaderFillColor = "#EEF1F2";
        private const string ErrorColor = "#AD6A5E";
        private const string ErrorFillColor = "#FBF3F1";
        private const string BorderColor = "#E3E8EA";
        private const string TextColor = "#2E3A3F";

        private static string Enc(object value) => WebUtility.HtmlEncode(value?.ToString() ?? "");

        public static string Build(DeviceViewModel dev)
        {
            var sb = new StringBuilder();
            sb.Append($@"
<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<style>
    body {{
        font-family: 'Segoe UI', sans-serif;
        color: {TextColor};
        margin: 0;
        padding: 24px 32px;
    }}
    h1 {{
        text-align: center;
        color: {AccentColor};
        font-size: 22px;
        font-weight: 600;
        letter-spacing: 0.3px;
        margin: 0 0 22px 0;
    }}
    h2 {{
        color: {AccentColor};
        font-size: 16px;
        font-weight: 600;
        margin: 0 0 10px 0;
    }}
    .card {{
        border: 1px solid {BorderColor};
        border-radius: 14px;
        box-shadow: 0 2px 6px rgba(0,0,0,0.05);
        padding: 16px 18px;
        margin-bottom: 18px;
        background: #ffffff;
    }}
    table {{
        width: 100%;
        border-collapse: collapse;
        border-radius: 10px;
        overflow: hidden;
    }}
    th, td {{
        text-align: left;
        padding: 8px 12px;
        border-bottom: 1px solid {BorderColor};
        font-size: 13px;
    }}
    tr:last-child td {{
        border-bottom: none;
    }}
    th {{
        background: {HeaderFillColor};
        color: {AccentColor};
        font-weight: 600;
    }}
    td.label {{
        color: {AccentColor};
        font-weight: 600;
        width: 45%;
    }}
    td.value {{
        font-weight: 600;
    }}
    .error-card {{
        border: 1px solid {BorderColor};
        border-left: 4px solid {ErrorColor};
        border-radius: 14px;
        background: {ErrorFillColor};
        padding: 14px 18px;
        margin-bottom: 14px;
    }}
    .error-title {{
        color: {ErrorColor};
        font-weight: 600;
        font-size: 14px;
        margin: 0 0 8px 0;
    }}
    .error-card table {{
        background: #ffffff;
    }}
    .errors-count {{
        color: {ErrorColor};
        font-size: 16px;
        font-weight: 600;
        margin: 0 0 12px 0;
    }}
</style>
</head>
<body>
<h1>{Enc(GetString("t_device_report"))}: {Enc(dev.Name)}</h1>

<div class='card'>
    <table>
        <tr><td class='label'>{Enc(GetString("t_serial_number"))}</td><td class='value'>{Enc(string.Join(".", dev.Serial))}</td></tr>
        <tr><td class='label'>{Enc(GetString("t_manufacturing_date"))}</td><td class='value'>{Enc(dev.ProductionDate)}</td></tr>
        <tr><td class='label'>{Enc(GetString("t_formed"))}</td><td class='value'>{Enc(DateTime.Now)}</td></tr>
    </table>
</div>
");

            if (dev.BbValues.Count > 0)
            {
                sb.Append($@"
<div class='card'>
    <h2>{Enc(GetString("t_common_black_box_data"))}</h2>
    <table>
        <tr><th>{Enc(GetString("t_name"))}</th><th>{Enc(GetString("t_value"))}</th></tr>
");
                foreach (var p in dev.BbValues)
                    sb.Append($"        <tr><td>{Enc(GetString($"bb_{p.Id}"))}</td><td class='value'>{Enc(p.Value)}</td></tr>\n");
                sb.Append("    </table>\n</div>\n");
            }

            if (dev.BbErrors.Count > 0)
            {
                sb.Append($"<div class='errors-count'>{Enc(GetString("t_errors_found"))}: {dev.BbErrors.Count}</div>\n");

                foreach (var e in dev.BbErrors)
                {
                    sb.Append($"<div class='error-card'>\n    <div class='error-title'>{Enc(e.Name)}</div>\n");
                    if (e.Variables.Count > 0)
                    {
                        sb.Append("    <table>\n");
                        foreach (var v in e.Variables)
                            sb.Append($"        <tr><td>{Enc(v.Name)}</td><td class='value'>{Enc(v.Value)}</td></tr>\n");
                        sb.Append("    </table>\n");
                    }
                    sb.Append("</div>\n");
                }
            }

            sb.Append("</body>\n</html>");
            return sb.ToString();
        }
    }
}
