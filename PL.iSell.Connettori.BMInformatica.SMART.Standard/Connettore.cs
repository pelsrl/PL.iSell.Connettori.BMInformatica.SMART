using System;
using System.Text;

namespace PL.iSell.Connettori.BMInformatica.SMART.Standard
{
    public class Connettore : SMART.Connettore
    {
        public Connettore()
        {
            this.Personalizzazione = "Standard";
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            new Connettore().Avvia();
        }
    }
}