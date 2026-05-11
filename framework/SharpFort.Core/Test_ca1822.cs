namespace Test {
    public class TestClass {
        // This method does not access instance data and should trigger CA1822
        public int Add(int a, int b) {
            return a + b;
        }
    }
}
namespace SharpFort.Core {
    public class TestClass {
        public int Add(int a, int b) {
            return a + b;
        }
    }
}
