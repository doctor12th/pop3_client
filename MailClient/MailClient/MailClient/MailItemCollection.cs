using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MailClient
{
  public class MailItemCollection : List<MailItemBase>
  {

    /// <summary>
    /// Добавляет общую часть письма в коллекцию
    /// </summary>
    /// <param name="source">MIME части письма с содержимым</param>
    public void AddItem(string source)
    {
      this.Add(new MailItemBase(source));
    }

  }
}
