using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Net.Http;

public class CPHInline
{
    public bool Execute()
    {
        string apiKey 			= args["chatGPT3APIkey"].ToString();
        string model 			= args["engine"].ToString();
        string voice 			= args["voice"].ToString();
        string brain 			= args["brain"].ToString();
        string tokens 			= args["tokens"].ToString();
        var discordWebhookUrl 	= args["discordWebhook"].ToString();
        	    
        	    
        // create a ChatGPTAPI instance with your API key
        ChatGPTAPI chatGPT = new ChatGPTAPI(apiKey);
        
        // get the message input from the arguments
        string messageInput = args["rawInput"].ToString();
        string user = args["user"].ToString();
        CPH.LogInfo("Requesting ChatGPT");
        
        // generate a response with the ChatGPTAPI
		    bool containsQuestionMark = messageInput.Contains("?");
        
        if(containsQuestionMark) { CPH.TtsSpeak(voice, user + " asks " + messageInput, false); }
        else {CPH.TtsSpeak(voice, user + " says " + messageInput, false);}
        
		
        string response = chatGPT.GenerateResponse(messageInput, user, model, brain, tokens);
        CPH.LogDebug("Response: " + response);
          
        if(response != "TooManyRequests")   
        {
          // send the response to the chat
          Root root = JsonConvert.DeserializeObject<Root>(response);
          string myString = root.choices[0].message.content;
          string myStringCleaned0 = myString.Replace(System.Environment.NewLine, " ");
          string mystringCleaned1 = Regex.Replace(myStringCleaned0, @"\r\n?|\n", " ");
          string myStringCleaned2 = Regex.Replace(mystringCleaned1, @"[\r\n]+", " ");
          string unescapedString = Regex.Unescape(myStringCleaned2);
          string finalGPT = unescapedString.Trim();

          CPH.SendMessage(finalGPT);
          CPH.LogInfo("GPT " + finalGPT);

          CPH.TtsSpeak(voice, "Well", false);
          CPH.TtsSpeak(voice, finalGPT, false);

          if(discordWebhookUrl != null)
          {
            var discordWebhookContent = new DiscordWebhookContent
            {
              content = $"**{user} asked {messageInput}**\n{finalGPT}"
            };
            var discordWebhookJson = JsonConvert.SerializeObject(discordWebhookContent);
            var discordWebhookHttpContent = new StringContent(discordWebhookJson, Encoding.UTF8, "application/json");
            var discordWebhookHttpClient = new HttpClient();
            discordWebhookHttpClient.PostAsync(discordWebhookUrl, discordWebhookHttpContent).Wait();
          }
		  }
      else
      {
        CPH.TtsSpeak(voice, "I seem to be overloaded right now, try again shortly", false);
      }
        return true;
    }
    
    private class DiscordWebhookContent
    {
        public string content { get; set; }
    }
    public class ChatGPTAPI
	  {
		private string _apiKey;
		private string _endpoint = "https://api.openai.com/v1/chat/completions";
		public ChatGPTAPI(string apiKey)
		{
			_apiKey = apiKey;
		}
		public string GenerateResponse(string prompt, string who, string model, string brain, string tokens)
		{
			// Create a request to the ChatGPT API
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_endpoint);
			request.Headers.Add("Authorization", "Bearer " + _apiKey);
			request.ContentType = "application/json";
			request.Method = "POST";
			// Build the request body
			
			string requestBody = "{\"model\": \"" + model + "\",\"max_tokens\": "+tokens+", \"messages\": [{\"role\": \"system\", \"content\": \" INSERT BEHAVIOR HERE\"}, {\"role\": \"user\", \"content\": \"" + prompt + "\"}]}";

			byte[] bytes = Encoding.UTF8.GetBytes(requestBody);
			request.ContentLength = bytes.Length;

			using (Stream requestStream = request.GetRequestStream())
			{
				requestStream.Write(bytes, 0, bytes.Length);
			}
			// Get the response from the ChatGPT API
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			

				if((int)response.StatusCode == 200)
				{
					string responseBody;
					using (Stream responseStream = response.GetResponseStream())
					{
						StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
						responseBody = reader.ReadToEnd();
					}
					return responseBody;
				}
				else
				{
					return response.StatusCode.ToString();
				}
		}
	}
}



public class Message
{
    public string role { get; set; }
    public string content { get; set; }
}

public class Choice
{
    public Message message { get; set; }
    public string finish_reason { get; set; }
    public int index { get; set; }
}

public class Usage
{
    public int prompt_tokens { get; set; }
    public int completion_tokens { get; set; }
    public int total_tokens { get; set; }
}

public class Root    {
    public string id { get; set; }
    public string @object { get; set; }
    public int created { get; set; }
    public string model { get; set; }
    public List<Choice> choices { get; set; }
    public Usage usage { get; set; }    }
