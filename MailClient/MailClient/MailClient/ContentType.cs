/*
 * Пример к статье: Получение почты по протоколу POP3 и обработка MIME
 * Автор: Алексей Немиро
 * http://aleksey.nemiro.ru
 * Специально для Kbyte.Ru
 * http://kbyte.ru
 * 27 августа 2011 года
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MailClient
{
  /// <summary>
  /// Объектное представление MIME-заголовка Content-Type
  /// </summary>
  public class ContentType : ParametersBase
  {

    private Encoding _CodePage = null;

    /// <summary>
    /// Кодировка
    /// </summary>
    public string Charset 
    { 
      get 
      {
        if (this.Parameters != null && this.Parameters.ContainsKey("charset"))
        {
          return this.Parameters["charset"];
        }
        return "utf-8"; // по умолчанию 
      } 
    }

    /// <summary>
    /// Граница (для разделения различного типа содержимого)
    /// </summary>
    public string Boundary 
    {
      get
      {
        if (this.Parameters != null && this.Parameters.ContainsKey("boundary"))
        {
          return this.Parameters["boundary"];
        }
        return String.Empty;
      }
    }

    /// <summary>
    /// Формат
    /// </summary>
    public string Format
    {
      get
      {
        if (this.Parameters != null && this.Parameters.ContainsKey("format"))
        {
          return this.Parameters["format"];
        }
        return String.Empty;
      }
    }

    /// <summary>
    /// Свойство содержит Encoding, полученный по имени Charset.
    /// Если возникнут проблемы с этим свойством, то нужно будет добавить проверку Charset на известное имя кодировки.
    /// </summary>
    public Encoding CodePage
    {
      get
      {
        if (_CodePage == null && !String.IsNullOrEmpty(this.Charset))
        {
          _CodePage = Encoding.GetEncoding(this.Charset);
        }
        else
        {
          _CodePage = Encoding.UTF8;
        }
        return _CodePage;
      }
    }

    public ContentType(string source): base(source) { }

  }
}
