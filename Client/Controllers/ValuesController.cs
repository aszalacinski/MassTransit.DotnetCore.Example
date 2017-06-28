namespace Client.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using MassTransit;
    using Sample.MessageTypes;
    using Microsoft.Extensions.DependencyInjection;

    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private IServiceProvider _serviceProvider;
        private IBus _busControl;

        public ValuesController(IBus busControl, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _busControl = busControl;
        }

        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{name}")]
        public IActionResult Get(string name)
        {
            _busControl.Publish<ISimpleRequest>(new { Timestamp = DateTime.UtcNow, CustomerId = name }).Wait();

            return Ok();
        }
        
        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
