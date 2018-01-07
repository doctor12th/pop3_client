
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MailClient
{
  /// <summary>
  /// Базовый класс для обработки параметров в значения MIME-заголовков
  /// </summary>
  public class ParametersBase
  {

    private string _Source = String.Empty;
    private string _Type = String.Empty;
    private ParametersCollection _Parameters = null;

    /// <summary>
    /// Источник данных
    /// </summary>
    public string Source { get { return _Source; } }

    /// <summary>
    /// Тип
    /// </summary>
    public string Type { get { return _Type; } }

    /// <summary>
    /// Коллекция параметров
    /// </summary>
    public ParametersCollection Parameters { get { return _Parameters; } }

    public ParametersBase(string source)
    {
      if (String.IsNullOrEmpty(source)) return;
      _Source = source;
      // ищем в источнике первое вхождение точки с запятой
      int typeTail = source.IndexOf(";");
      if (typeTail == -1)
      { // все содержимое источника является информацией о типа
        _Type = source;
        return; // параметров нет, выходим
      }
      _Type = source.Substring(0, typeTail);
      // парсим параметры
      string p = source.Substring(typeTail + 1, source.Length - typeTail - 1);
      _Parameters = new ParametersCollection();
      Regex myReg = new Regex(@"(?<key>.+?)=((""(?<value>.+?)"")|((?<value>[^\;]+)))[\;]{0,1}", RegexOptions.Singleline);
      MatchCollection mc = myReg.Matches(p);
      foreach (Match m in mc)
      {
        if (!_Parameters.ContainsKey(m.Groups["key"].Value))
        {
          _Parameters.Add(m.Groups["key"].Value.Trim(), m.Groups["value"].Value);
        }
        // параметров с одинаковыми именами по идеи быть не должно,
        // но если будут и если они необходимо, можно сделать также, как в парсере заголовков,
        // т.е. использовать объектный тип данных для значения параметра и сохранять его в виде вложенной коллекции
      }
    }

  }
}
