using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MailClient
{
  public class MailBinary : MailItemBase
  {
    private byte[] _Data = null;
    private Pop3Lib.MIME.ContentDisposition _ContentDisposition = null;

    /// <summary>
    /// Тело вложения
    /// </summary>
    public new byte[] Data
    {
      get { return _Data; }
    }

    /// <summary>
    /// Дополнительная информация о вложении
    /// </summary>
    public new Pop3Lib.MIME.ContentDisposition ContentDisposition
    {
      get { return _ContentDisposition; }
    }

    /// <summary>
    /// Имя файла, если есть
    /// </summary>
    public string FileName
    {
      get 
      {
        if (_ContentDisposition == null) return String.Empty;
        return _ContentDisposition.FileName; 
      }
    }

    public MailBinary(byte[] data, Pop3Lib.MIME.ContentDisposition contentDisposition) : base()
    {
      _Data = data;
      _ContentDisposition = contentDisposition;
    }
  }
}