using RestSharp;
using System;

namespace IdentificationApp.Source
{
    public class FaceIDResponse
    {
        public bool Match { get; set; }
        public int Identity { get; set; }
    }

    public interface IFaceIDCallback
    {
        void Match(int subject);
        void FirstTime();
        void Error(string error);
    }

    class FaceID
    {
        private const string URL = "http://127.0.0.1:5000/";

        private RestClient client;

        public FaceID()
        {
            client = new RestClient(URL);
        }

        public void Identify(int subject, IFaceIDCallback callback)
        {
            var request = new RestRequest("id", Method.GET);
            request.AddParameter("subject", subject);

            var asyncHandle = client.ExecuteAsync<FaceIDResponse>(request, response =>
            {
                if (response.Data != null)
                {
                    if (response.Data.Match) callback.Match(response.Data.Identity);
                    else callback.FirstTime();
                }
                else
                {
                    callback.Error(response.ErrorMessage);
                }
            });
        }
    }
}
