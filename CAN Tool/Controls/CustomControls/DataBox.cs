using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace CAN_Tool.CustomControls
{
    // Ввод 8 байт в шестнадцатеричном виде, всегда отображается как "FF FF FF FF FF FF FF FF".
    // Ширина фиксирована (8 байт = 16 hex-цифр), редактирование - посимвольный overtype: любая
    // правка (набор цифры, Backspace, Delete, вставка) заменяет цифры на конкретных позициях, но
    // никогда не меняет длину строки. Раньше правки делались через "вырезать всё нехекс, собрать
    // заново с пробелами" реактивно в TextChanged - при удалении одного hex-символа (нечётная
    // длина) это сдвигало все последующие байты на пол-байта, а сам обработчик TextChanged менял
    // Text изнутри себя же (реентерабельно). Теперь длина строки не меняется в принципе, поэтому
    // сдвигаться нечему.
    public class DataBox : TextBox
    {
        private const int ByteCount = 8;
        private const int DigitCount = ByteCount * 2; // 16 hex-цифр

        // Реентерабельность: True только пока сами меняем Text изнутри OnTextChanged - чтобы не
        // зациклиться на собственном же изменении (см. OnTextChanged).
        private bool normalizing;

        public DataBox()
        {
            Text = BuildDisplay(new string('F', DigitCount));
            MaxLength = DigitCount + (ByteCount - 1); // 16 hex-цифр + 7 пробелов между байтами

            PreviewTextInput += OnPreviewTextInput;
            PreviewKeyDown += OnPreviewKeyDown;
            DataObject.AddPastingHandler(this, OnPaste);
        }

        private static bool IsHexChar(char c) => Uri.IsHexDigit(c);

        // "XX XX XX XX XX XX XX XX" из 16 hex-цифр (ровно DigitCount символов).
        private static string BuildDisplay(string digits) =>
            string.Join(" ", Enumerable.Range(0, ByteCount).Select(i => digits.Substring(i * 2, 2)));

        // Позиция символа в отображаемой строке для логического индекса hex-цифры (0..15) -
        // после каждой пары цифр идёт пробел.
        private static int DisplayIndex(int digitIndex) => digitIndex + digitIndex / 2;

        // Обратное преобразование: какой логический индекс цифры (0..15) редактируется, если
        // каретка стоит в позиции caretIndex отображаемой строки. Позиция на пробеле или сразу
        // после него округляется к следующей цифре - так набор текста "перескакивает" пробелы
        // сам, без явного перемещения каретки пользователем.
        private static int DigitIndexAtCaret(int caretIndex) =>
            Math.Clamp(caretIndex - caretIndex / 3, 0, DigitCount - 1);

        // Заменяет одну hex-цифру по логическому индексу и ставит каретку сразу за ней (перескочив
        // пробел, если он там). Не меняет длину Text ни при каких digitIndex/hexDigit.
        private void SetDigit(int digitIndex, char hexDigit)
        {
            digitIndex = Math.Clamp(digitIndex, 0, DigitCount - 1);
            var pos = DisplayIndex(digitIndex);
            Text = Text.Remove(pos, 1).Insert(pos, char.ToUpperInvariant(hexDigit).ToString());
            var caret = pos + 1;
            if (caret % 3 == 2) caret++;
            CaretIndex = caret;
        }

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = true; // в любом случае не отдаём ввод стандартной обработке TextBox
            if (e.Text.Length != 1 || !IsHexChar(e.Text[0])) return;
            SetDigit(DigitIndexAtCaret(CaretIndex), e.Text[0]);
        }

        // Backspace/Delete не удаляют символы (это меняло бы длину строки и сдвигало байты) -
        // сбрасывают текущую hex-цифру в "F" (пустое/дефолтное значение поля - см. конструктор,
        // изначальный текст "FF FF...") и переставляют каретку, как в обычном hex-редакторе в
        // режиме overtype.
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back)
            {
                e.Handled = true;
                var digitIndex = Math.Max(0, DigitIndexAtCaret(CaretIndex) - 1);
                SetDigit(digitIndex, 'F');
                CaretIndex = DisplayIndex(digitIndex);
            }
            else if (e.Key == Key.Delete)
            {
                e.Handled = true;
                var digitIndex = DigitIndexAtCaret(CaretIndex);
                SetDigit(digitIndex, 'F');
                CaretIndex = DisplayIndex(digitIndex);
            }
        }

        // Раньше вставка отклонялась целиком, если в тексте был хоть один не-hex символ - в т.ч.
        // пробел, из-за чего нельзя было вставить даже то, что скопировано из этого же поля.
        // Теперь просто отфильтровываем всё лишнее (как и обычный ввод) и раскладываем оставшиеся
        // hex-цифры по позициям начиная от каретки - вставка всегда своя, штатную вставку TextBox
        // отменяем безусловно, чтобы длина строки не поменялась.
        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            e.CancelCommand();
            if (!e.DataObject.GetDataPresent(typeof(string))) return;

            var text = (string)e.DataObject.GetData(typeof(string));
            var hex = new string((text ?? "").Where(IsHexChar).ToArray());
            if (hex.Length == 0) return;
            if (hex.Length > DigitCount) hex = hex.Substring(0, DigitCount);

            var startDigit = DigitIndexAtCaret(CaretIndex);
            var lastDigit = startDigit;
            for (var i = 0; i < hex.Length && startDigit + i < DigitCount; i++)
            {
                lastDigit = startDigit + i;
                SetDigit(lastDigit, hex[i]);
            }
        }

        // Safety-net для правок, которые проходят мимо всего вышеперечисленного (например, Text
        // приходит из биндинга/конвертера в компактном виде без пробелов - см.
        // DataToStringConverter - или вставка через что-то помимо DataObjectPastingEventArgs):
        // нормализует Text к каноническому "XX XX ...", если он вдруг стал другим. Сравнение с уже
        // нормализованной строкой ограничивает рекурсию одним уровнем.
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            if (normalizing) return;

            var hex = new string(Text.Where(IsHexChar).ToArray());
            if (hex.Length > DigitCount) hex = hex.Substring(0, DigitCount);
            hex = hex.PadRight(DigitCount, 'F'); // "пусто" = "F", как и дефолтный текст поля
            var canonical = BuildDisplay(hex);
            if (Text == canonical) return;

            normalizing = true;
            try
            {
                var caret = Math.Min(CaretIndex, canonical.Length);
                Text = canonical;
                CaretIndex = caret;
            }
            finally { normalizing = false; }
        }
    }
}
