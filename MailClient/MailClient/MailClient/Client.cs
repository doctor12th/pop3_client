using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections;

namespace MailClient
{
    public class Client
    {

        private Socket _Socket = null;
        private string _Host = String.Empty;
        private int _Port = 110;
        private string _UserName = String.Empty;
        private string _Password = String.Empty;

        private Result _ServerResponse = new Result();
        private int _Index = 0;

        public int MessageCount = 0;
        public int MessagesSize = 0;

        public Client(string host, string userName, string password) : this(host, 110, userName, password) { }
        public Client(string host, int port, string userName, string password)
        {
            // проверка указания всех необходимых данных
            if (String.IsNullOrEmpty(host)) throw new Exception("Необходимо указать адрес pop3-сервера.");
            if (String.IsNullOrEmpty(userName)) throw new Exception("Необходимо указать логин пользователя.");
            if (String.IsNullOrEmpty(password)) throw new Exception("Необходимо указать пароль пользователя.");
            if (port <= 0) port = 110;
            // --

            this._Host = host;
            this._Password = password;
            this._Port = port;
            this._UserName = userName;


            this.Connect();
        }

        /// <summary>
        /// Метод осуществляет подключение к почтовому серверу
        /// </summary>
        public void Connect()
        {
            // получаем апишник сервера
            IPHostEntry myIPHostEntry = Dns.GetHostEntry(_Host);

            if (myIPHostEntry == null || myIPHostEntry.AddressList == null || myIPHostEntry.AddressList.Length <= 0)
            {
                throw new Exception("Не удалось определить IP-адрес по хосту.");
            }

            // получаем сетевую конечную точку
            IPEndPoint myIPEndPoint = new IPEndPoint(myIPHostEntry.AddressList[0], _Port);

            // инициализируем сокет
            _Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _Socket.ReceiveBufferSize = 1024; // размер приемного буфера в байтах

            // соединяемся с сервером
            WriteToLog("Соединяюсь с сервером {0}:{1}", _Host, _Port);
            _Socket.Connect(myIPEndPoint);
            // получаем ответ сервера
            string tmp2 = ReadLine();

            // выполняем авторизацию
            Command(String.Format("USER {0}", _UserName));
            string tmp = ReadLine();

            Command(String.Format("PASS {0}", _Password));
            // сервер обычно проводит окончательную проверку
            // только после указания пароля, 
            // так что проверять ответ на ошибки до этого момента
            // смысла нет
            _ServerResponse = ReadLine();
            if (_ServerResponse.IsError)
            {
                throw new Exception(_ServerResponse.ServerMessage);
            }

            // запрашиваем статистику почтового ящика
            Command("STAT");
            _ServerResponse = ReadLine();
            if (_ServerResponse.IsError)
            {
                throw new Exception(_ServerResponse.ServerMessage);
            }

            _ServerResponse.ParseStat(out this.MessageCount, out this.MessagesSize);
        }

        /// <summary>
        /// Метод завершает сеанс связи с сервером
        /// </summary>
        public void Close()
        {
            if (_Socket == null) { return; }
            Command("QUIT");
            ReadLine();
            _Socket.Close();
        }

        /// <summary>
        /// Фукнция возвращает заголовки указанного письма
        /// </summary>
        /// <param name="index">Индекс письма, начиная с 1</param>
        public Dictionary<string, object> GetMailHeaders(int index)
        {
            if (index > this.MessageCount)
            {
                throw new Exception(String.Format("Индекс должен быть от 1 и не больше {0}", this.MessageCount));
            }
            Command(String.Format("TOP {0} 0", index));
            _ServerResponse = ReadToEnd();
            if (_ServerResponse.IsError)
            {
                throw new Exception(_ServerResponse.ServerMessage);
            }
            MailItem m;
            _ServerResponse.ParseMail(out m);
            return m.Headers;
        }

        /// <summary>
        /// Следующее письмо
        /// </summary>
        public bool NextMail(out MailItem m)
        {
            m = null;
            // следующее письмо
            _Index++;
            // если больше писем нет, возвращаем false
            if (_Index > this.MessageCount) return false;
            Command(String.Format("RETR {0}", _Index));
            _ServerResponse = ReadToEnd();
            if (_ServerResponse.IsError)
            {
                throw new Exception(_ServerResponse.ServerMessage);
            }
            _ServerResponse.ParseMail(out m);
            return true;
        }

        /// <summary>
        /// Метод удаляет текущее письмо
        /// </summary>
        public void Delete()
        {
            Delete(_Index);
        }

        /// <summary>
        /// Метод удаляет указанное письмо
        /// </summary>
        public void Delete(int index)
        {
            if (index > this.MessageCount)
            {
                throw new Exception(String.Format("Индекс должен быть от 1 и не больше {0}", this.MessageCount));
            }
            Command(String.Format("DELE {0}", index));
            _ServerResponse = ReadLine();
            if (_ServerResponse.IsError)
            {
                throw new Exception(_ServerResponse.ServerMessage);
            }
        }

        /// <summary>
        /// Метод отправляет команду почтовому серверу
        /// </summary>
        /// <param name="cmd">Команда</param>
        public void Command(string cmd)
        {
            if (_Socket == null) throw new Exception("Соединение с сервером не установлено. Откройте соединение методом Connect.");
            WriteToLog("Команда: {0}", cmd);// логирование
            byte[] b = System.Text.Encoding.ASCII.GetBytes(String.Format("{0}\r\n", cmd));
            if (_Socket.Send(b, b.Length, SocketFlags.None) != b.Length)
            {
                throw new Exception("При отправке данных удаленному серверу произошла ошибка...");
            }
        }

        /// <summary>
        /// Считывает первую строку ответа сервера из буфера
        /// </summary>
        public string ReadLine()
        {
            byte[] b = new byte[_Socket.ReceiveBufferSize];
            StringBuilder result = new StringBuilder(_Socket.ReceiveBufferSize);
            int s = 0;
            // если будут проблемы с Poll, увеличьте время с 1000000 мс до ...
            while (_Socket.Poll(10000000, SelectMode.SelectRead) && (s = _Socket.Receive(b, _Socket.ReceiveBufferSize, SocketFlags.None)) > 0)
            {
                result.Append(System.Text.Encoding.ASCII.GetChars(b, 0, s));
            }
            WriteToLog(result.ToString().TrimEnd("\r\n".ToCharArray()));// логирование

            return result.ToString().TrimEnd("\r\n".ToCharArray());
        }

        /// <summary>
        /// Читает и возвращает все содержимое ответа сервера из буфера
        /// </summary>
        public string ReadToEnd()
        {
            byte[] b = new byte[_Socket.ReceiveBufferSize];
            StringBuilder result = new StringBuilder(_Socket.ReceiveBufferSize);
            int s = 0;
            // если будут проблемы с Poll, увеличьте время с 1000000 мс до ...
            while (_Socket.Poll(1000, SelectMode.SelectRead) && ((s = _Socket.Receive(b, _Socket.ReceiveBufferSize, SocketFlags.None)) > 0))
            {
                result.Append(System.Text.Encoding.ASCII.GetChars(b, 0, s));
            }

            // логирование
            if (result.Length > 0 && result.ToString().IndexOf("\r\n") != -1)
            {
                WriteToLog(result.ToString().Substring(0, result.ToString().IndexOf("\r\n")));
            }
            // --

            return result.ToString();
        }

        private void WriteToLog(string msg, params object[] args)
        {
            Console.WriteLine("{0}: {1}", DateTime.Now, String.Format(msg, args));
        }
    }
}
