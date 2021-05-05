//using microsoft.aspnetcore.mvc;
//using system;
//using auctiontigerscraper;
//using system.collections.generic;
//using system.linq;
//using system.threading.tasks;
//using system.io;

//// for more information on enabling web api for empty projects, visit https://go.microsoft.com/fwlink/?linkid=397860

//namespace auctionscraperapi.controllers
//{
//    [route("api/[controller]")]
//    [apicontroller]
//    public class vehicleimagescontroller : controllerbase
//    {
//        private readonly auctionscraper _auctionscraper;
//        public vehicleimagescontroller(auctionscraper auctiontigerscraper)
//        {
//            _auctionscraper = auctiontigerscraper;
//        }
//        // get: api/<vehicleimagescontroller>
//        [httpget]
//        public iactionresult get(guid id)
//        {
//            downloadmanager downloadmanager = new downloadmanager(path.gettemppath());
//            _auctionscraper.
//        }

//        // get api/<vehicleimagescontroller>/5
//        [httpget("{id}")]
//        public string get(int id)
//        {
//            return "value";
//        }

//        // post api/<vehicleimagescontroller>
//        [httppost]
//        public void post([frombody] string value)
//        {
//        }

//        // put api/<vehicleimagescontroller>/5
//        [httpput("{id}")]
//        public void put(int id, [frombody] string value)
//        {
//        }

//        // delete api/<vehicleimagescontroller>/5
//        [httpdelete("{id}")]
//        public void delete(int id)
//        {
//        }
//    }
//}
