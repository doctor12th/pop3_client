using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Text.RegularExpressions;

namespace MailClient
{
    /// <summary>
    /// Базовый класс для писем или частей писем
    /// </summary>
    public class MailItemBase
    {

        private string _Source = String.Empty;
        private Dictionary<string, object> _Headers = null;
        private ContentType _ContentType = null;
        private ContentDisposition _ContentDisposition = null;
        private string _ContentTransferEncoding = String.Empty;
        private object _Data = null;

        /// <summary>
        /// Исходный текст письма (MIME)
        /// </summary>
        public string Source
        {
            get { return _Source; }
        }

        /// <summary>
        /// Коллекция MIME-заголовков
        /// </summary>
        public Dictionary<string, object> Headers
        {
            get { return _Headers; }
        }

        

        /// <summary>
        /// Тип содержимого
        /// </summary>
        public ContentType ContentType
        {
            get { return _ContentType; }
        }

        /// <summary>
        /// Дополнительная информация о содержимом
        /// </summary>
        public ContentDisposition ContentDisposition
        {
            get { return _ContentDisposition; }
        }

        /// <summary>
        /// Тип кодирования содержимого
        /// </summary>
        public string ContentTransferEncoding
        {
            get { return _ContentTransferEncoding; }
        }

        /// <summary>
        /// Содержимое
        /// </summary>
        public object Data
        {
            get { return _Data; }
        }

        /// <summary>
        /// Возвращает true, если в текущей части письма нет никаких данных
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return _Data == null ||
                  (_Data.GetType() == typeof(string) && String.IsNullOrEmpty(_Data.ToString())) ||
                  (_Data.GetType() == typeof(byte[]) && ((byte[])_Data).Length <= 0) ||
                  (_Data.GetType() == typeof(MailItemCollection) && ((MailItemCollection)_Data).Count <= 0);
            }
        }

        /// <summary>
        /// Возвращает true, если текущая часть письма содержит текстовые данные
        /// </summary>
        public bool IsText
        {
            get { return _Data != null && _Data.GetType() == typeof(string); }
        }

        /// <summary>
        /// Возвращает true, если текущая часть письма содержит бинарные данные
        /// </summary>
        public bool IsBinary
        {
            get { return _Data != null && _Data.GetType() == typeof(byte[]); }
        }

        /// <summary>
        /// Возвращает true, если текущая часть письма содержит вложенные части
        /// </summary>
        public bool IsMultipart
        {
            get { return _Data != null && _Data.GetType() == typeof(MailItemCollection); }
        }

        public MailItemBase() { }

        public MailItemBase(string source)
        {
            if (String.IsNullOrEmpty(source))
            {
                throw new Exception("Необходимо указать источник для создания экземпляра письма или части письма.");
            }
            // передаем исходный текст письма в соответствующее свойство текущего объекта
            _Source = source;
            // выделяем заголовки (до первых двух переводов строк)
            int headersTail = source.IndexOf("\r\n\r\n"); // еще пригодится
            string h = String.Empty;
            if (headersTail == -1)
            { // хвост не найден, значит в теле сообщения только заголовки
                h = source;
            }
            else
            { // хвост найден, отделяем заголовки
                h = source.Substring(0, headersTail);
            }
            _Headers = ParseHeaders(h);
            

            if (headersTail == -1) return; // если тела письма нет, то смысла его искать тоже нет

            // заголовки обработаны, теперь обрабатываем тело письма
            // выделяем тело с конца заголовков
            string b = source.Substring(headersTail + 4, source.Length - headersTail - 4); // 4 = "\r\n\r\n".Length
                                                                                           // смотрим, какая кодировка используется в письме
            if (_Headers.ContainsKey("Content-Transfer-Encoding"))
            {
                _ContentTransferEncoding = _Headers["Content-Transfer-Encoding"].ToString().ToLower();
            }
            // тип содержимого
            if (_Headers.ContainsKey("Content-Type"))
            {
                _ContentType = new ContentType(_Headers["Content-Type"].ToString());
            }
            else
            {
                _ContentType = new ContentType("");// создаем пустой тип содержимого, чтобы не возникало исключений
            }
            // дополнительная информация о содержимом, если данные бинарные 
            if (_Headers.ContainsKey("Content-Disposition"))
            {
                _ContentDisposition = new ContentDisposition(_Headers["Content-Disposition"].ToString());
            }

            // смотрим, какой тип содержимого в данном письме
            if (_ContentType.Type.StartsWith("multipart"))
            {
                // смешанный тип данных (multipart)
                ParseMultiPart(b); // парсим
            }
            else if (_ContentType.Type.StartsWith("application") || _ContentType.Type.StartsWith("image") || _ContentType.Type.StartsWith("video") || _ContentType.Type.StartsWith("audio"))
            {
                // бинарный тип содержимого
                if (_ContentTransferEncoding != "base64")
                {
                    throw new Exception("Для бинарного содержимого ожидается тип кодирования Base64");
                }
                _Data = Convert.FromBase64String(b);
            }
            else
            { // другой, скорей всего текст
                _Data = DecodeContent(_ContentTransferEncoding, b);
            }
        }

        /// <summary>
        /// Метод парсит различные части письма
        /// </summary>
        private void ParseMultiPart(string b)
        {
            Regex myReg = new Regex(String.Format(@"(--{0})([^\-]{{2}})", _ContentType.Boundary), RegexOptions.Multiline);
            MatchCollection mc = myReg.Matches(b);
            // создаем коллекцию частей разношерстного содержимого
            MailItemCollection items = new MailItemCollection();
            // делаем поиск каждой части сообщения
            for (int i = 0; i <= mc.Count - 1; i++)
            {
                int start = mc[i].Index + String.Format("--{0}", _ContentType.Boundary).Length;
                int len = 0;
                if (i + 1 > mc.Count - 1)
                {
                    len = b.Length - start;
                }
                else
                {
                    len = (mc[i + 1].Index - 1) - start;
                }
                string part = b.Substring(start, len).Trim("\r\n".ToCharArray());
                int partTail = 0;
                if ((partTail = part.LastIndexOf(String.Format("--{0}--", _ContentType.Boundary))) != -1)
                {
                    part = part.Substring(0, partTail);
                }
                items.AddItem(part);
            }
            // передаем коллекцию в свойство Data текущего экземпляра объекта
            _Data = items;
        }

        /// <summary>
        /// Функция парсит заголовки и возвращает коллекцию Dictionary
        /// </summary>
        /// <param name="h">Источник, из которого нужно получить заголовки</param>
        private Dictionary<string, object> ParseHeaders(string h)
        {
            Dictionary<string, object> result = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);
            // декодируем текстовые данные в заголовках
            h = Regex.Replace(h, @"([\x22]{0,1})\=\?(?<cp>[\w\d\-]+)\?(?<ct>[\w]{1})\?(?<value>[^\x3f]+)\?\=([\x22]{0,1})", HeadersEncode, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            // удаляем лишные пробелы
            h = Regex.Replace(h, @"([\r\n]+)^(\s+)(.*)?$", " $3", RegexOptions.Multiline);
            // а теперь парсим заголовки и заносим их в коллекцию
            Regex myReg = new Regex(@"^(?<key>[^\x3A]+)\:\s{1}(?<value>.+)$", RegexOptions.Multiline);
            MatchCollection mc = myReg.Matches(h);
            foreach (Match m in mc)
            {
                string key = m.Groups["key"].Value;
                if (result.ContainsKey(key))
                {
                    // если указанный ключ уже есть в коллекции,
                    // то проверяем тип данных
                    if (result[key].GetType() == typeof(string))
                    {
                        // тип данных - строка, преобразуем в коллекцию
                        ArrayList arr = new ArrayList();
                        // добавляем в коллекцию первый элемент
                        arr.Add(result[key]);
                        // добавляем в коллекцию текущий элемент
                        arr.Add(m.Groups["value"].Value);
                        // вставляем коллекцию элементов в найденный заголовок
                        result[key] = arr;
                    }
                    else
                    {
                        // считаем, что тип данных - коллекция, 
                        // добавляем найденный элемент
                        ((ArrayList)result[key]).Add(m.Groups["value"].Value);
                    }
                }
                else
                {
                    // такого ключа нет, добавляем
                    result.Add(key, m.Groups["value"].Value.TrimEnd("\r\n ".ToCharArray()));
                }
            }
            // возвращаем коллекцию полученных заголовков
            return result;
        }

        /// <summary>
        /// Функция обратного вызова, обрабатывается в методе ParseHeaders, производит декодирование данных в заголовках, в соответствии с найденными атрибутами.
        /// </summary>
        private string HeadersEncode(Match m)
        {
            string result = String.Empty;
            Encoding cp = Encoding.GetEncoding(m.Groups["cp"].Value);
            if (m.Groups["ct"].Value.ToUpper() == "Q")
            {
                // кодируем из Quoted-Printable
                result = ParseQuotedPrintable(m.Groups["value"].Value);
            }
            else if (m.Groups["ct"].Value.ToUpper() == "B")
            {
                // кодируем из Base64
                result = cp.GetString(Convert.FromBase64String(m.Groups["value"].Value));
            }
            else
            {
                // такого быть не должно, оставляем текст как есть
                result = m.Groups["value"].Value;
            }
            return result; //ConvertCodePage(result, cp);
        }

        /// <summary>
        /// Функция производит декодирование Quoted-Printable.
        /// </summary>
        /// <param name="source">Текст для декодирования</param>
        private string ParseQuotedPrintable(string source)
        {
            source = source.Replace("_", " ");
            source = Regex.Replace(source, @"(\=)([^\dABCDEFabcdef]{2})", "");
            return Regex.Replace(source, @"\=(?<char>[\d\w]{2})", QuotedPrintableEncode);
        }
        /// <summary>
        /// Функция обратного вызова, используется в функции ParseQuotedPrintable при обработке найденных совпадений.
        /// </summary>
        private string QuotedPrintableEncode(Match m)
        {
            return ((char)int.Parse(m.Groups["char"].Value, System.Globalization.NumberStyles.AllowHexSpecifier)).ToString();
        }

        /// <summary>
        /// Конвертирует текст из кодировки источника в кодировку по умолчанию
        /// </summary>
        private string ConvertCodePage(string source, Encoding source_encoding)
        {
            if (source_encoding == Encoding.Default) return source;
            return Encoding.Default.GetString(source_encoding.GetBytes(source));
        }
        /// <summary>
        /// Конвертирует текст из массива байт из кодировки источника в кодировку по умолчанию
        /// </summary>
        private string ConvertCodePage(byte[] source, Encoding source_encoding)
        {
            if (source_encoding == Encoding.Default) return Encoding.Default.GetString(source);
            return Encoding.Default.GetString(Encoding.Default.GetBytes(source_encoding.GetString(source)));
        }

        /// <summary>
        /// Функция декодирует указанное содержимое
        /// </summary>
        /// <param name="contentTransferEncoding">Тип кодирования</param>
        /// <param name="source">Содержимое, которые нужно декодировать</param>
        private string DecodeContent(string contentTransferEncoding, string source)
        {
            if (contentTransferEncoding == "base64")
            {
                return ConvertCodePage(Convert.FromBase64String(source), _ContentType.CodePage);
            }
            else if (contentTransferEncoding == "quoted-printable")
            {
                return ConvertCodePage(ParseQuotedPrintable(source), _ContentType.CodePage);
            }
            else
            { //"8bit", "7bit", "binary"
              // считаем, что это обычный текст
                return ConvertCodePage(source, _ContentType.CodePage);
            }
        }

        /// <summary>
        /// Хелпер-фукнция, возвращает текстовые данные текущей части письма.
        /// Если письмо содержит данные отличные от текстовых, функция возвращает пустую строку.
        /// </summary>
        public string GetText()
        {
            if (!this.IsText) return String.Empty;
            return _Data.ToString();
        }

        /// <summary>
        /// Хелпер-фукнция, возвращает массив байт, содержащий бинарные данные текущей части письма.
        /// Если письмо содержит данные отличные от бинарных, функция возвращает null.
        /// </summary>
        public byte[] GetBinary()
        {
            if (!this.IsBinary) return null;
            return (byte[])_Data;
        }

        /// <summary>
        /// Хелпер-фукнция, возвращает коллекцию вложенных частей текущей части письма.
        /// Если письмо содержит данные отличные от коллекции частей, функция возвращает null.
        /// </summary>
        public MailItemCollection GetItems()
        {
            if (!this.IsMultipart) return null;
            return (MailItemCollection)_Data;
        }

    }
}
