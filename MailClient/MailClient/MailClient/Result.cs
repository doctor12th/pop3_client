
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace MailClient
{
    /// <summary>
    /// Универсальный обработчик ответов почтового сервера
    /// </summary>
    [DefaultProperty("Source")]
    public class Result
    {
        /// <summary>
        /// Исходные данные
        /// </summary>
        public string Source { get; set; }
        /// <summary>
        /// Показатель ошибки в ответе сервера
        /// </summary>
        public bool IsError { get; set; }
        /// <summary>
        /// Сообщение сервера (первая строка)
        /// </summary>
        public string ServerMessage { get; set; }
        /// <summary>
        /// Тело ответа сервера, исключая код ответа (IsError) и сообщение (ServerMessage)
        /// </summary>
        public string Body { get; set; }

        public Result() { }
        public Result(string source)
        {
            this.Source = source;
            // обрабатываем ответ
            this.IsError = source.StartsWith("-ERR"); // ошибка, или нет
                                                      // получаем отдельно сообщение о результате выполнения команды
            Regex myReg = new Regex(@"(\+OK|\-ERR)\s{1}(?<msg>.*)?", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (myReg.IsMatch(source))
            {
                this.ServerMessage = myReg.Match(source).Groups["msg"].Value;
            }
            else
            {
                this.ServerMessage = source;
            }
            // если есть, получаем тело сообщения, удаляя сообщение сервера и лишние маркеры протокола
            if (source.IndexOf("\r\n") != -1)
            {
                this.Body = source.Substring(source.IndexOf("\r\n") + 2, source.Length - source.IndexOf("\r\n") - 2);
                if (this.Body.IndexOf("\r\n\r\n.\r\n") != -1)
                {
                    this.Body = this.Body.Substring(0, this.Body.IndexOf("\r\n\r\n.\r\n"));
                }
            }
            // --
        }

        /// <summary>
        /// Реализия неявного оператора преобразования
        /// </summary>
        public static implicit operator Result(string value)
        {
            return new Result(value);
        }

        /// <summary>
        /// Получает из ответа сервера информацию о количестве писем и их размере.
        /// Используется только при команде STAT
        /// </summary>
        /// <param name="messagesCount">Передает количество сообщений (начиная с 1)</param>
        /// <param name="messagesSize">Передает общий размер сообщений</param>
        public void ParseStat(out int messagesCount, out int messagesSize)
        {
            Regex myReg = new Regex(@"(?<count>\d+)\s+(?<size>\d+)");
            Match m = myReg.Match(this.Source);
            int.TryParse(m.Groups["count"].Value, out messagesCount);
            int.TryParse(m.Groups["size"].Value, out messagesSize);
        }

        /// <summary>
        /// Парсим ответ сервера на команду UIDL
        /// </summary>
        /// <param name="b">Исходный текст</param>
        /// <param name="messageCount">Количество сообщений</param>
        /// <returns></returns>
        public List<string> ParseUids(string b, int messageCount)
        {
            string pattern = @"\s\d+\s";
            if (b != null && b != "")
            {
                string b1 = b.Replace("\r\n", " ");
                string[] uids = new string[messageCount];
                string[] separator = { " " };
                b1 = Regex.Replace(b1, pattern, " ");
                uids = b1.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                List<string> uid = new List<string>();
                foreach (var item in uids)
                {
                    uid.Add(item);
                }
                uid.RemoveAt(0);
                uid.RemoveAt(uid.IndexOf("."));
                return uid;
            }
            else MessageBox.Show("There is no answer from server.");
            return null;
            
            
        }
        /// <summary>
        /// Метод передает обработанное письмо на основе данных, полученных от почтового сервера
        /// </summary>
        /// <param name="m">Переменная, в которую будет передано обработанное письмо</param>
        public void ParseMail(out MailItem m)
        {
            m = new MailItem(this.Body);
        }

    }
}
