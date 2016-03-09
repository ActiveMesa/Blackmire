using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaygroundCs
{
  public class Color
  {
    byte r, g, b, a;

    public static Color Red = new Color(255,0,0,255);

    private Color(byte r, byte g, byte b, byte a)
    {
      this.r = r;
      this.g = g;
      this.b = b;
      this.a = a;
    }
  }

  class Program
  {
    public void MyMethod(Color c)
    {
      
    }

    static void Main(string[] args)
    {
      
    }
  }
}
