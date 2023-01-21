using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace Avalonia.SourceGenerator.CompositionGenerator
{
    public partial class Generator
    {
        private const string ListProxyTemplate = @"
class Template
{
        private ServerListProxyHelper<ItemTypeName, ServerItemTypeName> _list = null!;

        void ServerListProxyHelper<ItemTypeName, ServerItemTypeName>.IRegisterForSerialization.RegisterForSerialization() => RegisterForSerialization();

        public List<ItemTypeName>.Enumerator GetEnumerator() => _list.GetEnumerator();

        IEnumerator<ItemTypeName> IEnumerable<ItemTypeName>.GetEnumerator() =>
            GetEnumerator();
        
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) _list).GetEnumerator();

        public void Add(ItemTypeName item)
        {
            OnBeforeAdded(item);
            _list.Add(item);
            OnAdded(item);
        }

        public void Clear()
        {
            OnBeforeClear();
            _list.Clear();
            OnClear();
        }

        public bool Contains(ItemTypeName item) => _list.Contains(item);

        public void CopyTo(ItemTypeName[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        public bool Remove(ItemTypeName item)
        {
            var removed = _list.Remove(item);
            if(removed)
                OnRemoved(item);
            return removed;
        }

        public int Count => _list.Count;

        public bool IsReadOnly => _list.IsReadOnly;

        public int IndexOf(ItemTypeName item) => _list.IndexOf(item);

        public void Insert(int index, ItemTypeName item)
        {
            OnBeforeAdded(item);
            _list.Insert(index, item);
            OnAdded(item);
        }

        public void RemoveAt(int index)
        {
            var item = _list[index];
            _list.RemoveAt(index);
            OnRemoved(item);
        }

        public ItemTypeName this[int index]
        {
            get => _list[index];
            set
            {
                var old = _list[index];
                OnBeforeReplace(old, value);
                _list[index] = value;
                OnReplace(old, value);
            }
        }

        partial void OnBeforeAdded(ItemTypeName item);
        partial void OnAdded(ItemTypeName item);
        partial void OnRemoved(ItemTypeName item);
        partial void OnBeforeClear();
        partial void OnBeforeReplace(ItemTypeName oldItem, ItemTypeName newItem);
        partial void OnReplace(ItemTypeName oldItem, ItemTypeName newItem);
        partial void OnClear();
        private protected override void SerializeChangesCore(BatchStreamWriter writer)
        {{
            _list.Serialize(writer);
            base.SerializeChangesCore(writer);
        }}
";

        private static ClassDeclarationSyntax AppendListProxy(GList list, ClassDeclarationSyntax cl)
        {

            var itemType = list.ItemType;
            var serverItemType = ServerName(itemType);

            cl = cl.AddBaseListTypes(SimpleBaseType(
                    ParseTypeName("ServerListProxyHelper<" + itemType + ", " + serverItemType + ">.IRegisterForSerialization")),
                SimpleBaseType(ParseTypeName("IList<" + itemType + ">"))
            );
            var code = ListProxyTemplate.Replace("ListTypeName", list.Name)
                .Replace("ItemTypeName", itemType);

            var parsed = ParseCompilationUnit(code);
            var parsedClass = (ClassDeclarationSyntax)parsed.Members.First();

            cl = cl.AddMembers(parsedClass.Members.ToArray());

            var defs = cl.Members.OfType<MethodDeclarationSyntax>().First(m => m.Identifier.Text == "InitializeDefaults");

            cl = cl.ReplaceNode(defs.Body!, defs.Body!.AddStatements(

                ParseStatement($"_list = new ServerListProxyHelper<{itemType}, {serverItemType}>(this);")));
          
            return cl;
        }
        
    }
}
