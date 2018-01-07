using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MailClient
{
  public class MailText: MailItemBase
  {

    public enum TextFormat
    {
      /// <summary>
      /// Обычный текст
      /// </summary>
      Plain,
      /// <summary>
      /// Гипертекст
      /// </summary>
      Html,
      /// <summary>
      /// RTF
      /// </summary>
      Rich,
      Unknown
    }

    private TextFormat _Format = TextFormat.Unknown;
    private string _Data = String.Empty;
        public string Data
        {
            get { return _Data; }
        }


        /// <summary>
        /// Формат письма (обычный текст, html или rtf)
        /// </summary>
        public TextFormat Format
    {
      get { return _Format; }
    }

    /// <summary>
    /// Текст письма
    /// </summary>
    

    public MailText(string data, TextFormat format): base()
    {
      _Data = data;
      _Format = format;
    }

  }
}