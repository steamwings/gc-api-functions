namespace FunctionsTests.Extensions
{
    public static class ObjectExtensions
    {
        public static T GetPropertyValue<T>(this object obj, string property)
        {
            return (T) obj.GetType().GetProperty(property).GetValue(obj, null);
        }
    }
}
