using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace CAN_Tool.CustomControls
{
   

    public class DataBox : TextBox
    {

      
        private void UpdateTextFromByteArray(byte[] byteArray)
        {
            string hexString = BitConverter.ToString(byteArray).Replace("-", " ");
            Text = hexString;
        }
        // Массив байт, к которому привязан TextBox
        private byte[] _byteArray = new byte[8];

        // Свойство для привязки массива байт
        public byte[] ByteArray
        {
            get => _byteArray;
            set
            {
                if (value == null || value.Length != 8)
                    throw new ArgumentException("Byte array must have exactly 8 elements.");

                _byteArray = value;
                UpdateTextFromByteArray();
            }
        }


        // Конструктор
        public DataBox()
        {


            // Инициализация массива значениями 0xFF
            for (int i = 0; i < 8; i++)
            {
                _byteArray[i] = 0xFF;
            }

            // Ограничиваем длину текста (8 байт * 2 символа + 7 пробелов)
            MaxLength = 23;

            // Обработка изменения текста
            TextChanged += OnTextChanged;

            // Обработка ввода, чтобы разрешить только шестнадцатеричные символы
            PreviewTextInput += OnPreviewTextInput;

            // Обработка вставки текста
            DataObject.AddPastingHandler(this, OnPaste);

            // Инициализация текста
            UpdateTextFromByteArray();
        }

        // Обновление текста в TextBox на основе массива байт
        private void UpdateTextFromByteArray()
        {
            string hexString = BitConverter.ToString(_byteArray).Replace("-", " ");
            Text = hexString;
        }

        // Обновление массива байт на основе текста в TextBox
        private void UpdateByteArrayFromText()
        {
            string text = Text.Replace(" ", ""); // Удаляем пробелы
            if (text.Length != 16) // 16 символов = 8 байт
                return;

            for (int i = 0; i < 8; i++)
            {
                string byteString = text.Substring(i * 2, 2);
                if (byte.TryParse(byteString, System.Globalization.NumberStyles.HexNumber, null, out byte result))
                {
                    _byteArray[i] = result;
                }
                else
                {
                    _byteArray[i] = 0xFF; // В случае ошибки сбрасываем байт в 0xFF
                }
            }
        }

        // Обработка изменения текста
        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            // Сохраняем позицию курсора
            int caretIndex = CaretIndex;

            // Удаляем все пробелы и невалидные символы
            string text = new string(Text.Where(c => IsHexChar(c)).ToArray());

            // Ограничиваем длину строки до 16 символов (8 байт)
            if (text.Length > 16)
            {
                text = text.Substring(0, 16);
            }

            // Вставляем пробелы между парами символов
            string formattedText = string.Join(" ", Enumerable.Range(0, (text.Length + 1) / 2)
                .Select(i => text.Substring(i * 2, Math.Min(2, text.Length - i * 2))));

            // Обновляем текст в TextBox
            Text = formattedText;

            // Восстанавливаем позицию курсора
            CaretIndex = caretIndex;

            // Обновляем массив байт
            UpdateByteArrayFromText();
        }

        // Обработка ввода, чтобы разрешить только шестнадцатеричные символы
        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Проверяем, является ли ввод допустимым
            if (!e.Text.All(IsHexChar))
            {
                e.Handled = true;
                return;
            }

            // Заменяем символ справа от курсора
            int caretIndex = CaretIndex;
            if (caretIndex % 3 == 2)
                caretIndex++;

                if (caretIndex < Text.Length)
            {
                // Удаляем символ справа от курсора

                Text = Text.Remove(caretIndex, 1).Insert(caretIndex, e.Text.ToUpper());

                    CaretIndex = caretIndex + 1; // Перемещаем курсор вперед
            }

            e.Handled = true; // Отменяем стандартную обработку ввода
        }

        // Обработка вставки текста
        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!text.All(IsHexChar))
                {
                    e.CancelCommand(); // Отменяем вставку, если текст содержит недопустимые символы
                }
            }
            else
            {
                e.CancelCommand(); // Отменяем вставку, если данные не являются текстом
            }
        }

        // Проверка, является ли символ допустимым шестнадцатеричным символом
        private bool IsHexChar(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
        }
    }
}
