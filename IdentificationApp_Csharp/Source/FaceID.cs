using RestSharp;

namespace IdentificationApp.Source
{
    public class FaceIDResponse
    {
        public bool Match { get; set; }
        public int Identity { get; set; }
    }

    class FaceID
    {
        private const string URL = "http://127.0.0.1:5000/";

        private MainWindow main;
        private RestClient client;

        public FaceID(MainWindow main)
        {
            this.main = main;
            client = new RestClient(URL);
        }

        public void Identify(int subject)
        {
            var request = new RestRequest("id", Method.GET);
            request.AddParameter("subject", subject);

            var asyncHandle = client.ExecuteAsync<FaceIDResponse>(request, response =>
            {
                if (response.Data != null)
                {
                    if (response.Data.Match) main.Matched(response.Data.Identity);
                    else main.FirstTime();
                }
            });
        }

        public void Process(int subject)
        {
            var request = new RestRequest("process", Method.GET);
            request.AddParameter("subject", subject);

            var asyncHandle = client.ExecuteAsync<FaceIDResponse>(request, response =>
            {
                return;
            });
        }

        public void Train()
        {
            var request = new RestRequest("train", Method.GET);
            var asyncHandle = client.ExecuteAsync<FaceIDResponse>(request, response =>
            {
                main.EndTraining();
            });
        }
    }
}
