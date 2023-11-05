

namespace Core_SK_Patch
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HotSwappableAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class TranspilerAuxliliaryAttribute : Attribute
    {
        public string TargetClass;
        public string TargetMethod;
        public TranspilerAuxliliaryAttribute(string @class, string method) 
        {
            TargetClass = @class;
            TargetMethod = method;
        }
    }
}
