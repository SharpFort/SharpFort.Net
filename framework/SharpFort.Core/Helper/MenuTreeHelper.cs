using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpFort.Core.Helper
{
    /// <summary>
    /// 菜单树辅助类 - 支持升序排序
    /// </summary>
    public static class MenuTreeHelper
    {
        public static List<T> SetTree<T>(List<T> list, Action<T> action = null!)
        {
            if (list is not null && list.Count > 0)
            {
                IList<T> result = new List<T>();
                Guid pid = list.Min(m => (m as ITreeModel<T>)!.ParentId);
                IList<T> t = list.Where(m => (m as ITreeModel<T>)!.ParentId == pid).ToList();
                foreach (T model in t)
                {
                    if (action is not null)
                    {
                        action(model);
                    }
                    result.Add(model);
                    var item = model as ITreeModel<T>;
                    IList<T> children = list.Where(m => (m as ITreeModel<T>)!.ParentId == item!.Id).ToList();
                    if (children.Count > 0)
                    {
                        SetTreeChildren(list, children, model, action!);
                    }
                }
                // 改为升序排序，并增加稳定排序键 Id
                return result.OrderBy(m => (m as ITreeModel<T>)!.OrderNum).ThenBy(m => (m as ITreeModel<T>)!.Id).ToList();
            }
            return null!;
        }

        private static void SetTreeChildren<T>(IList<T> list, IList<T> children, T model, Action<T> action = null!)
        {
            var mm = model as ITreeModel<T>;
            mm!.Children = new List<T>();
            foreach (T item in children)
            {
                if (action is not null)
                {
                    action(item);
                }
                mm.Children.Add(item);
                var _item = item as ITreeModel<T>;
                IList<T> _children = list.Where(m => (m as ITreeModel<T>)!.ParentId == _item!.Id).ToList();
                if (_children.Count > 0)
                {
                    SetTreeChildren(list, _children, item, action!);
                }
            }
            // 改为升序排序，并增加稳定排序键 Id
            mm.Children = mm.Children.OrderBy(m => (m as ITreeModel<T>)!.OrderNum).ThenBy(m => (m as ITreeModel<T>)!.Id).ToList();
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
