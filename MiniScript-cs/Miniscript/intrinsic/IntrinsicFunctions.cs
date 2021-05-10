using System;

namespace Miniscript.intrinsic {

    public class IntrinsicFunctions {

        private void PrivateFunc() {
            Console.WriteLine("This is private function - not for injecting");
        }
        
        public double Abs_temp(double x) {
            return Math.Abs(x);
        }
        
        public double Abs_2(float x) {
            return Math.Abs(x);
        }

        public double Abs(double x = 0) {
            return Math.Abs(x);
        }
        
        public double Atan(double y = 0, double x = 1) {
            return x == 1.0 ? Math.Atan(y) : Math.Atan2(y, x);
        }

    }

}