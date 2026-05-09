namespace SharpFort.Core.Helper
{
    public static class TreeHelper
    {
        public static List<T> SetTree<T>(List<T> list, Action<T> action = null!)
        {
            if (list is not null && list.Count > 0)
            {
                List<T> result = [];
                Guid pid = list.Min(m => (m as ITreeModel<T>)!.ParentId);
                List<T> t = [.. list.Where(m => (m as ITreeModel<T>)!.ParentId == pid)];
                foreach (T model in t)
                {
                    if (action is not null)
                    {
                        action(model);
                    }
                    result.Add(model);
                    ITreeModel<T>? item = model as ITreeModel<T>;
                    List<T> children = [.. list.Where(m => (m as ITreeModel<T>)!.ParentId == item!.Id)];
                    if (children.Count > 0)
                    {
                        SetTreeChildren(list, children, model, action!);
                    }
                }
                return [.. result.OrderByDescending(m => (m as ITreeModel<T>)!.OrderNum)];
            }
            return null!;
        }
        private static void SetTreeChildren<T>(IList<T> list, IList<T> children, T model, Action<T> action = null!)
        {
            ITreeModel<T>? mm = model as ITreeModel<T>;
            mm!.Children = [];
            foreach (T item in children)
            {
                if (action is not null)
                {
                    action(item);
                }
                mm.Children.Add(item);
                ITreeModel<T>? _item = item as ITreeModel<T>;
                List<T> _children = [.. list.Where(m => (m as ITreeModel<T>)!.ParentId == _item!.Id)];
                if (_children.Count > 0)
                {
                    SetTreeChildren(list, _children, item, action!);
                }
            }
            mm.Children = [.. mm.Children.OrderByDescending(m => (m as ITreeModel<T>)!.OrderNum)];
        }


        public interface ITreeModel<T>
        {
            public Guid Id { get; set; }
            public Guid ParentId { get; set; }
            public int OrderNum { get; set; }

            public List<T>? Children { get; set; }
        }
    }

}
