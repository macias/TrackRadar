using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;

namespace TestRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            var test = new TrackRadar.Tests.TurnTest();
            test.WRONG_PickingMiddleTurnTest();

            //RunAllTests();
            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }

        private static void RunAllTests()
        {
            var executor = new Program();
            executor.RunAll(typeof(TrackRadar.Tests.KalmanTest).Assembly);
            executor.RunAll(typeof(Geo.Tests.DistanceTests).Assembly);
        }

        public void RunAll(Assembly asm)
        {
            foreach (Type type in asm.GetTypes())
            {
                if (type.GetCustomAttribute(typeof(TestClassAttribute)) != null)
                {
                    RunTests(type);
                }
            }
        }

        private void RunTests(Type type)
        {
            object instance = Activator.CreateInstance(type);
            foreach (MethodInfo minfo in type.GetMethods())
            {
                if (minfo.GetCustomAttribute(typeof(TestMethodAttribute)) == null)
                    continue;

                Console.WriteLine($"{type.Name}.{minfo.Name}");
                var attrs = minfo.GetCustomAttributes<DataRowAttribute>();
                if (attrs.Any())
                {
                    foreach (DataRowAttribute a in attrs)
                        minfo.Invoke(instance, a.Data);
                }
                else
                    minfo.Invoke(instance, new object[] { });
            }
        }
    }
}
