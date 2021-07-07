using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using CoreBot.Dialogs;

namespace CoreBot.QnA
{

    public class Metadata
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class Answer
    {
        public IList<string> questions { get; set; }
        public string answer { get; set; }
        public double score { get; set; }
        public int id { get; set; }
        public string source { get; set; }
        public IList<object> keywords { get; set; }
        public IList<Metadata> metadata { get; set; }
    }

    public class QnAAnswer
    {
        public IList<Answer> answers { get; set; }
    }

    [Serializable]
    public class QnAMakerService
    {

        public double topAnswerScore; // (04/10) JDM: Setting QnA Confidence Score Threshold

        async Task<string> Post(string uri, string body, string endpointKey)
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                request.Headers.Add("Authorization", "EndpointKey " + endpointKey);

                var response = await client.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<string> GetAnswer(string question, bool withBackup)
        {
            string qnaServiceResourceName = "https://" + "usfvirtualassistantqna" + ".azurewebsites.net";
            string knowledgeBaseId = "3786144c-2846-4082-8507-96922a4b03b9";
            string endpointKey = "ed80f5a3-7f52-40d7-8b04-7afe2906edb9";

            string uri = qnaServiceResourceName + "/qnamaker/knowledgebases/" + knowledgeBaseId + "/generateAnswer";
            string questionJSON = "{\"question\": \"" + question.Replace("\"", "'") + "\"}";

            var response = await Post(uri, questionJSON, endpointKey);
            QnAAnswer answers = null;
            try
            {
                answers = JsonConvert.DeserializeObject<QnAAnswer>(response);
            }
            catch { }

            if (answers?.answers?.Count > 0)
            {
                topAnswerScore = answers.answers[0].score; // (04/10) JDM: Setting QnA Confidence Score Threshold
                return answers.answers[0].answer;
            }
            else
            {
                if (withBackup) 
                {
                    qnaServiceResourceName = "https://" + "usfva-qna" + ".azurewebsites.net";
                    knowledgeBaseId = "ebc1ea32-db33-4735-ba24-287774cb0cfa";
                    endpointKey = " 2dc2eea8-be7c-4043-9528-bc77d41a4163";
                    uri = qnaServiceResourceName + "/qnamaker/knowledgebases/" + knowledgeBaseId + "/generateAnswer";
                    questionJSON = "{\"question\": \"" + question.Replace("\"", "'") + "\"}";

                    response = await Post(uri, questionJSON, endpointKey);
                    try
                    {
                        answers = JsonConvert.DeserializeObject<QnAAnswer>(response);
                    }
                    catch { }

                    if (answers?.answers?.Count > 0)
                    {
                        topAnswerScore = answers.answers[0].score; // (04/10) JDM: Setting QnA Confidence Score Threshold
                        return answers.answers[0].answer;
                    }
                }
                return MainDialog.DEFAULT_QNA;
            }
        }
    }
    // *** Feature-9 }
}
