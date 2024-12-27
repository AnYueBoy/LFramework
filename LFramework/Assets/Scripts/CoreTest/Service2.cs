using InjectionCore;
using UnityEngine;

namespace CoreTest
{
    public class Service2 : IService2
    {
        [Inject] public Service1 Service1 { get; set; }
        private readonly Service1 _service1;

        // public Service2(Service1 service1)
        // {
        //     _service1 = service1;
        // }

        public void Init()
        {
            Service1.DebugService1();
        }

        public void Service2Interface()
        {
        }
    }
}