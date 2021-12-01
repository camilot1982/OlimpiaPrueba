//PRUEBA OLIMPIA
//La función de la aplicación actual es calcular el saldo final de las cuentas de un "banco", para esto se consume un servicio que devuelve 
//las transacciones realizas a la cuentas.

//Paso 1: Hacer funcionar la aplicación. Debido al aumento de transacciones y al  colocar al servicio con SSL la aplicación actual esta fallando.
//Paso 2: Estructurar mejor el codigo. Uso de patrones, buenas practicas, etc.
//Paso 3: Optimizar el codigo, como se menciono en el paso 1 el aumento de transacciones ha causado que el calculo de los saldos se demore demasiado.
//Paso 4: Adicionar una barra de progreso al formulario. Actualizar la barra con el progreso del proceso, evitando bloqueos del GUI.


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Comunes;

namespace WindowsLiderEntrega
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        ///  Cliente del servicio Web
        /// </summary>
        public ServicioPrueba.ServiceClient client
        {
            get
            {
                return new ServicioPrueba.ServiceClient();
            }
        }

        int contador = 0;

        /// <summary>
        /// 
        /// </summary>
        public ServicioPrueba.Transaccion[] transaccion { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCalcular_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate (object x, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };
            this.transaccion = this.client.GetData("usuariop", "passwordp");

            // Agrupa por Cuenta.
            var res = this.transaccion.Select(x => new { CuentaOrigen = x.CuentaOrigen })
                                      .Distinct()
                                      .ToList();

            // Calcula los saldos de acuerdo a la cuenta origen previamente agrupada
            List<ServicioPrueba.Saldo> saldos = (from n in res
                                                 select new ServicioPrueba.Saldo
                                                 {
                                                     CuentaOrigen = n.CuentaOrigen,
                                                     SaldoCuenta = SaldoCuenta(n.CuentaOrigen, res.Count()),
                                                 }).ToList();

            sw.Stop();
            lblTiempoTotal.Text = sw.ElapsedMilliseconds.ToString();

            //Enviamos los saldos finales
            client.SaveData("usuariop", "passwordp", saldos.ToArray());

        }

        /// <summary>
        /// Itera por ciuenta cada movimiento y este lo suma dependientdo el tipo (Debito o Credito)
        /// </summary>
        /// <param name="cuentaOrigen"></param>
        /// <returns></returns>
        double SaldoCuenta(long cuentaOrigen, int progress)
        {
            contador++;
            contador = (contador * 100 / progress);
            progressBar1.Value = contador;
            return this.transaccion
                    .Where(x => x.CuentaOrigen == cuentaOrigen)
                    .Sum(x => x.ValorTransaccion = Movimiento(x.CuentaOrigen, x.TipoTransaccion, x.ValorTransaccion));
        }

        /// <summary>
        /// Calcula el valor del movimiento
        /// </summary>
        /// <param name="cuentaActual"></param>
        /// <param name="tipoTransaccion"></param>
        /// <param name="valorTransaccion"></param>
        /// <returns></returns>
        double Movimiento(long cuentaActual, string tipoTransaccion, double valorTransaccion)
        {
            double saldo = 0;
            var claveCifrado = this.client.GetClaveCifradoCuenta("usuariop", "passwordp", cuentaActual);
            var movimientoActual = Helper.Desencripta(claveCifrado, tipoTransaccion);
            double comision = Helper.CalcularComision(Convert.ToInt64(valorTransaccion));
            if (movimientoActual == "Debito")
            {
                saldo -= valorTransaccion;
            }
            else
            {
                saldo += valorTransaccion - comision;
            }
            return saldo;
        }

        

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
