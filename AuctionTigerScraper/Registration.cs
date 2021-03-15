using System.Text.RegularExpressions;

namespace AuctionTigerScraper
{
    public class Registration
    {
        public string State { get; set; }
        public string RTO { get; set; }
        public string Series { get; set; }
        public string Number { get; set; }
        public override string ToString()
        {
            return State + RTO + Series + Number;
        }
        public string ToString(string format)
        {
            return State + format + RTO + format + Series + format + Number;
        }
        public Registration(string State,string RTO, string Series, string Number)
        {
            this.State = State;
            this.RTO = RTO;
            this.Series = Series;
            this.Number = Number;
        }
        public Registration(string RawNumber)
        {
            var match = Regex.Match(RawNumber,@"(?<State>[A-Z]{2})[- ]*?(?<RTO>[A-Z\d]{1,2})[- ]*?(?<Series>[A-Z\d]{1,2})[- ]*?(?<Number>[\d]{4}) ",RegexOptions.IgnoreCase|RegexOptions.ExplicitCapture);
            State = match.Groups["State"].Value;
            RTO = match.Groups["RTO"].Value;
            Series = match.Groups["Series"].Value;
            Number = match.Groups["Number"].Value;
        }
    }
}
