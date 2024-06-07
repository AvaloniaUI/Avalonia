namespace Tmds.DBus.Protocol;

interface IMethodHandlerDictionary
{
    void AddMethodHandlers(IReadOnlyList<IMethodHandler> methodHandlers);
    void AddMethodHandler(IMethodHandler methodHandler);
    void RemoveMethodHandler(string path);
    void RemoveMethodHandlers(IEnumerable<string> paths);
}