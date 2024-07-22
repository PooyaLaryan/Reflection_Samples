

using System.Reflection;

var aaa = Assembly.GetEntryAssembly();
 
var assemblies = AppDomain.CurrentDomain.GetAssemblies();

var a = Assembly.GetExecutingAssembly().GetTypes();
var b = Assembly.GetExecutingAssembly().GetExportedTypes();
var c= Assembly.GetExecutingAssembly().GetForwardedTypes();

var d = GetAllEntities();    

Console.ReadKey();


List<string> GetAllEntities()
{
    return AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
          .Where(x => x.IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract && x.IsClass)
          .Select(x => x.Name).Where(x=>x == "Class1" || x == "Class2").ToList();
}