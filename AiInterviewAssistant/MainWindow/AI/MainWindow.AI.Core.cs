using System;
using System.Net.Http;

namespace AiInterviewAssistant
{
    public partial class MainWindow
    {
        private int _aiRequestInProgress = 0;

        private static readonly HttpClient _openRouterClient =
             new HttpClient
             {
                 Timeout = TimeSpan.FromSeconds(90)
             };
    }
}
