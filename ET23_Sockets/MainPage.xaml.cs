using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.SpeechRecognition;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// La plantilla de elemento Página en blanco está documentada en https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0xc0a

namespace ET23_Sockets
{
    /// <summary>
    /// Página vacía que se puede usar de forma independiente o a la que se puede navegar dentro de un objeto Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Comunicaciones coms;


        public MainPage()
        {
            this.InitializeComponent();
            coms = new Comunicaciones();

            textCon.Text = coms.Connect("kona2.alc.upv.es", 8081);
            //textCon.Text = coms.Connect("127.0.0.1", 8081);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialogo = new MessageDialog("¿Seguro que quieres conectar?", "Confirmación");
            dialogo.Commands.Add(new UICommand("Si") { Id = 0 });
            dialogo.Commands.Add(new UICommand("No") { Id = 1 });
            dialogo.DefaultCommandIndex = 1;
            dialogo.CancelCommandIndex = 1;
            var result = await dialogo.ShowAsync();

            if ((int)result.Id == 0)
            {
                string valor;
                textCon.Text = coms.Enviar(0, 0, 0, out valor);
                textInfo.Text = valor;
                rotacionManecilla.Rotation = coms.dirvent;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter != null)
            {
                var comandArgs = e.Parameter as VoiceCommandActivatedEventArgs;
                if (comandArgs != null)
                {
                    SpeechRecognitionResult sRR = comandArgs.Result;
                    string voiceC = sRR.RulePath[0];
                    if (voiceC == "informacio")
                    {
                        string valor;
                        textCon.Text = coms.Enviar(0, 0, 0, out valor);
                        textInfo.Text = valor;
                        rotacionManecilla.Rotation = coms.dirvent;
                    }
                }
            }
        }
    }
}
