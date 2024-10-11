using System;

namespace PL.iSell.Connettori.BMInformatica.SMART.Standard
{
    internal static class Program
    {
        static Program()
        {
            CosturaUtility.Initialize();
        }

        [STAThread]
        private static void Main()
        {
            new Connettore().Avvia();
        }
    }
}