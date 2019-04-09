using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Intent;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BasicBot.CogService
{
    public static class SpeechService
    {
        private static string SUBSCRIPTION_KEY = "4df75f434f5542fbb5f8539c5258e013";
        private static string TOKENFETCHURI = "https://centralus.api.cognitive.microsoft.com/sts/v1.0/issuetoken";
        private static string REGION = "centralus";
        private static readonly string FILE_NAME = @"C:\Users\aessalhi\source\repos\speechTest\speechTest\bin\Debug\netcoreapp2.1\sample.wav";
        private static readonly string HOST = "https://centralus.tts.speech.microsoft.com/cognitiveservices/v1";

        public static async Task TextToSpeechAsync(string text)
        {
            // Gets an access token
            string accessToken;
            // Add your subscription key here
            // If your resource isn't in centralus, change the endpoint
            Authentication auth = new Authentication(TOKENFETCHURI, SUBSCRIPTION_KEY);
            try
            {
                accessToken = await auth.FetchTokenAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return;
            }

            // Create SSML document.
            XDocument body = new XDocument(
                    new XElement("speak",
                        new XAttribute("version", "1.0"),
                        new XAttribute(XNamespace.Xml + "lang", "en-US"),
                        new XElement("voice",
                            new XAttribute(XNamespace.Xml + "lang", "en-US"),
                            new XAttribute(XNamespace.Xml + "gender", "Female"),
                            new XAttribute("name", "Microsoft Server Speech Text to Speech Voice (en-US, Jessa24kRUS)"),
                            text)));

            using (HttpClient client = new HttpClient())
            {
                using (HttpRequestMessage request = new HttpRequestMessage())
                {
                    // Set the HTTP method
                    request.Method = HttpMethod.Post;
                    // Construct the URI
                    request.RequestUri = new Uri(HOST);
                    // Set the content type header
                    request.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/ssml+xml");
                    // Set additional header, such as Authorization and User-Agent
                    request.Headers.Add("Authorization", "Bearer " + accessToken);
                    request.Headers.Add("Connection", "Keep-Alive");
                    // Update your resource name
                    request.Headers.Add("User-Agent", "YOUR_RESOURCE_NAME");
                    // Audio output format. See API reference for full list.
                    request.Headers.Add("X-Microsoft-OutputFormat", "riff-24khz-16bit-mono-pcm");
                    // Create a request

                    using (HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        // Asynchronously read the response
                        using (Stream dataStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            using (FileStream fileStream = new FileStream(@"sample.wav", FileMode.Create, FileAccess.Write, FileShare.Write))
                            {
                                await dataStream.CopyToAsync(fileStream).ConfigureAwait(false);

                                fileStream.Close();
                            }

                            // playSound(@"D:\projet\botCustomerV2-src\sample.wav");
                        }
                    }
                }
            }
        }

        public static void PlaySound()
        {
            string fileName = @"D:\projet\botCustomerV2-src\sample.wav";
            using (var waveOut = new WaveOutEvent())
            using (var wavReader = new WaveFileReader(fileName))
            {
                waveOut.Init(wavReader);
                waveOut.Play();
                while (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(100);
                }
                waveOut.Dispose();
            }
        }

        public static async Task RecognizeSpeechAsync()
        {
            var config = SpeechConfig.FromSubscription(SUBSCRIPTION_KEY, REGION);

            // Creates a speech recognizer.
            using (var recognizer = new SpeechRecognizer(config))
            {
                Console.WriteLine("Say something...");

                var result = await recognizer.RecognizeOnceAsync();

                // Checks result.
                if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    Console.WriteLine($"We recognized: {result.Text}");
                }
                else if (result.Reason == ResultReason.NoMatch)
                {
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = CancellationDetails.FromResult(result);
                    Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you update the subscription info?");
                    }
                }
            }
        }
    }
    public class Authentication
    {
        private string subscriptionKey;
        private string tokenFetchUri;

        public Authentication(string tokenFetchUri, string subscriptionKey)
        {
            if (string.IsNullOrWhiteSpace(tokenFetchUri))
            {
                throw new ArgumentNullException(nameof(tokenFetchUri));
            }
            if (string.IsNullOrWhiteSpace(subscriptionKey))
            {
                throw new ArgumentNullException(nameof(subscriptionKey));
            }
            this.tokenFetchUri = tokenFetchUri;
            this.subscriptionKey = subscriptionKey;
        }

        public async Task<string> FetchTokenAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", this.subscriptionKey);
                UriBuilder uriBuilder = new UriBuilder(this.tokenFetchUri);

                HttpResponseMessage result = await client.PostAsync(uriBuilder.Uri.AbsoluteUri, null).ConfigureAwait(false);
                return await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }
    }
}
