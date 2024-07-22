using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TempSampleCode
{
    public class AppDomainTypeFinder : ITypeFinder
    {
        protected IHafFileProvider _fileProvider;

        private readonly bool _ignoreReflectionErrors = true;

        public virtual AppDomain App => AppDomain.CurrentDomain;

        public bool LoadAppDomainAssemblies { get; set; } = true;


        public IList<string> AssemblyNames { get; set; } = new List<string>();


        public string AssemblySkipLoadingPattern { get; set; } = "^System|^mscorlib|^Microsoft|^AjaxControlToolkit|^Antlr3|^Autofac|^AutoMapper|^Castle|^ComponentArt|^CppCodeProvider|^DotNetOpenAuth|^EntityFramework|^EPPlus|^FluentValidation|^ImageResizer|^itextsharp|^log4net|^MaxMind|^MbUnit|^MiniProfiler|^Mono.Math|^MvcContrib|^Newtonsoft|^NHibernate|^nunit|^Org.Mentalis|^PerlRegex|^QuickGraph|^Recaptcha|^Remotion|^RestSharp|^Rhino|^Telerik|^Iesi|^TestDriven|^TestFu|^UserAgentStringLibrary|^VJSharpCodeProvider|^WebActivator|^WebDev|^WebGrease|^NWebsec|^IdentityModel|^MlkPwgen|^RedLock|^NLog|^IdentityServer4|^SixLabors|^iTextSharp|^BundlerMinifier|^Swashbuckle|^AspNetCoreRateLimit|^Orleans|^MicroElements|^netstandard|^ILFieldBuilder|^StackExchange|^EFCore|^HtmlAgilityPack|^Humanizer|^Imageflow|^MassTransit|^RabbitMQ|^OpenIddict|^NetTopologySuite|^ncrontab|^Automatonymous|^SQLitePCLRaw|^Polly|^Pipelines|^NewId|^GreenPipes|^Anonymously|^dotnet|^Azure|^Dia2Lib|^DiagramBuilder|^Elastic|^Hashids|^Npgsql|^Pastel|^Serialize.Linq|^Serilog|^OSExtensions|^TraceReloggerLib|^JsonNet";


        public string AssemblyRestrictToLoadingPattern { get; set; } = ".*";


        public AppDomainTypeFinder(IHafFileProvider fileProvider = null)
        {
            _fileProvider = fileProvider ?? CommonHelper.DefaultFileProvider;
        }

        private void AddAssembliesInAppDomain(List<string> addedAssemblyNames, List<Assembly> assemblies)
        {
            foreach (Assembly item in (from d in AppDomain.CurrentDomain.GetAssemblies()
                                       orderby d.FullName
                                       select d).ToList())
            {
                if (Matches(item.FullName) && !addedAssemblyNames.Contains(item.FullName))
                {
                    assemblies.Add(item);
                    addedAssemblyNames.Add(item.FullName);
                }
            }
        }

        protected virtual void AddConfiguredAssemblies(List<string> addedAssemblyNames, List<Assembly> assemblies)
        {
            foreach (string assemblyName in AssemblyNames)
            {
                Assembly assembly = Assembly.Load(assemblyName);
                if (!addedAssemblyNames.Contains(assembly.FullName))
                {
                    assemblies.Add(assembly);
                    addedAssemblyNames.Add(assembly.FullName);
                }
            }
        }

        public virtual bool Matches(string assemblyFullName)
        {
            if (!Matches(assemblyFullName, AssemblySkipLoadingPattern))
            {
                return Matches(assemblyFullName, AssemblyRestrictToLoadingPattern);
            }

            return false;
        }

        protected virtual bool Matches(string assemblyFullName, string pattern)
        {
            return Regex.IsMatch(assemblyFullName, pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        protected virtual void LoadMatchingAssemblies(string directoryPath)
        {
            List<string> list = new List<string>();
            foreach (Assembly assembly in GetAssemblies())
            {
                list.Add(assembly.FullName);
            }

            if (!_fileProvider.DirectoryExists(directoryPath))
            {
                return;
            }

            string[] files = _fileProvider.GetFiles(directoryPath, "*.dll");
            foreach (string assemblyFile in files)
            {
                try
                {
                    AssemblyName assemblyName = AssemblyName.GetAssemblyName(assemblyFile);
                    if (Matches(assemblyName.FullName) && !list.Contains(assemblyName.FullName))
                    {
                        try
                        {
                            App.Load(assemblyName);
                        }
                        catch
                        {
                        }
                    }
                }
                catch (BadImageFormatException ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }
        }

        protected virtual bool DoesTypeImplementOpenGeneric(Type type, Type openGeneric)
        {
            try
            {
                Type genericTypeDefinition = openGeneric.GetGenericTypeDefinition();
                Type[] array = type.FindInterfaces((Type objType, object? objCriteria) => true, null);
                foreach (Type type2 in array)
                {
                    if (type2.IsGenericType)
                    {
                        return genericTypeDefinition.IsAssignableFrom(type2.GetGenericTypeDefinition());
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public IEnumerable<Type> FindClassesOfType<T>(bool onlyConcreteClasses = true)
        {
            return FindClassesOfType(typeof(T), onlyConcreteClasses);
        }

        public IEnumerable<Type> FindClassesOfType(Type assignTypeFrom, bool onlyConcreteClasses = true)
        {
            return FindClassesOfType(assignTypeFrom, GetAssemblies(), onlyConcreteClasses);
        }

        public IEnumerable<Type> FindClassesOfType<T>(IEnumerable<Assembly> assemblies, bool onlyConcreteClasses = true)
        {
            return FindClassesOfType(typeof(T), assemblies, onlyConcreteClasses);
        }

        public IEnumerable<Type> FindClassesOfType(Type assignTypeFrom, IEnumerable<Assembly> assemblies, bool onlyConcreteClasses = true)
        {
            List<Type> list = new List<Type>();
            try
            {
                foreach (Assembly assembly in assemblies)
                {
                    Type[] array = null;
                    try
                    {
                        array = assembly.GetTypes();
                    }
                    catch
                    {
                        if (!_ignoreReflectionErrors)
                        {
                            throw;
                        }
                    }

                    if (array == null)
                    {
                        continue;
                    }

                    Type[] array2 = array;
                    foreach (Type type in array2)
                    {
                        if ((!assignTypeFrom.IsAssignableFrom(type) && (!assignTypeFrom.IsGenericTypeDefinition || !DoesTypeImplementOpenGeneric(type, assignTypeFrom))) || type.IsInterface)
                        {
                            continue;
                        }

                        if (onlyConcreteClasses)
                        {
                            if (type.IsClass && !type.IsAbstract)
                            {
                                list.Add(type);
                            }
                        }
                        else
                        {
                            list.Add(type);
                        }
                    }
                }

                return list;
            }
            catch (ReflectionTypeLoadException ex)
            {
                string text = string.Empty;
                Exception[] loaderExceptions = ex.LoaderExceptions;
                foreach (Exception ex2 in loaderExceptions)
                {
                    text = text + ex2.Message + Environment.NewLine;
                }

                throw new Exception(text, ex);
            }
        }

        public IEnumerable<Type> FindGenericEntityTypeConfigurations(params Type[] types)
        {
            List<Type> list = new List<Type>();
            try
            {
                foreach (Assembly assembly in GetAssemblies())
                {
                    list.AddRange(assembly.GetTypes().Where(delegate (Type type)
                    {
                        Type? baseType = type.BaseType;
                        return (object)baseType != null && baseType.IsGenericType && types.Contains(type.BaseType.GetGenericTypeDefinition());
                    }));
                }

                return list;
            }
            catch (ReflectionTypeLoadException ex)
            {
                string text = string.Empty;
                Exception[] loaderExceptions = ex.LoaderExceptions;
                foreach (Exception ex2 in loaderExceptions)
                {
                    text = text + ex2.Message + Environment.NewLine;
                }

                throw new Exception(text, ex);
            }
        }

        public virtual IList<Assembly> GetAssemblies()
        {
            List<string> addedAssemblyNames = new List<string>();
            List<Assembly> list = new List<Assembly>();
            if (LoadAppDomainAssemblies)
            {
                AddAssembliesInAppDomain(addedAssemblyNames, list);
            }

            AddConfiguredAssemblies(addedAssemblyNames, list);
            return list;
        }
    }
}
