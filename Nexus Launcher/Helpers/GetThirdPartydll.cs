using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus_Launcher.Helpers
{
    public class GetThirdPartydll
    {
        public static Task<string> GetThirdPartyReferences()
        {
            StringBuilder sb =
                new StringBuilder();

            var assemblies =
                AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Where(a =>
                    {
                        string name =
                            a.GetName().Name;

                        return !name.StartsWith("System") &&
                               !name.StartsWith("Microsoft") &&
                               !name.StartsWith("mscorlib");
                    })
                    .OrderBy(a =>
                        a.GetName().Name);

            foreach (var asm in assemblies)
            {
                sb.AppendLine(
                    $"{asm.GetName().Name} " +
                    $"v{asm.GetName().Version}");
            }

            return Task.FromResult(sb.ToString());
        }
    }
}
