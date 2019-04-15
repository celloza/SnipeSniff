using Newtonsoft.Json;
using SnipeSharp.Common;
using SnipeSharp.Endpoints.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnipeSharp.Endpoints.ExtendedManagers
{
    public class ComponentEndpointManager<T> : EndPointManager<Component>
    {
        // Explicitly pass components as the endpoint, ignoring what the client gives us
        public ComponentEndpointManager(IRequestManager reqManager, string endPoint) : base(reqManager, "components")
        {

        }

        public IRequestResponse Checkout(ICommonEndpointModel item)
        {
            IRequestResponse result;
            string response = _reqManager.Post(string.Format("{0}/{1}/checkout", _endPoint, item.Id), item);
            result = JsonConvert.DeserializeObject<RequestResponse>(response);
            return result;
        }

        public IRequestResponse Checkin(ICommonEndpointModel item)
        {
            IRequestResponse result;
            string response = _reqManager.Post(string.Format("{0}/{1}/checkin", _endPoint, item.Id), item);
            result = JsonConvert.DeserializeObject<RequestResponse>(response);
            return result;
        }
    }
}
