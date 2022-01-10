using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Models.Permissions
{
	public class PermissionsList : Dictionary<Guid, PermissionsListItem>// : IDictionary<Role, IList<Permission>>
	{
		public void Add(Role role, IList<Permission> permissions)
		{
			base.Add(role.Id, new PermissionsListItem() { Role = role, Permissions = permissions });
		}
		//private Dictionary<Role, IList<Permission>> Permissions = new();

		//public void Remove(Role role)
		//{
		//	this.Permissions.Remove(role);
		//}

		//public void Add(Role role, IList<Permission> permissions)
		//{
		//	this.Permissions.Add(role, permissions);
		//}

		//public Dictionary<Role, IList<Permission>>.Enumerator GetEnumerator()
		//{
		//	return this.Permissions.GetEnumerator();
		//}
				
		//IEnumerator IEnumerable.GetEnumerator()
		//{
		//	return this.Permissions.GetEnumerator();
		//}

		//IEnumerator<KeyValuePair<Role, IList<Permission>>> IEnumerable<KeyValuePair<Role, IList<Permission>>>.GetEnumerator()
		//{
		//	return ((IEnumerable<KeyValuePair<Role, IList<Permission>>>)this.Permissions).GetEnumerator();
		//}

		//public bool ContainsKey(Role key)
		//{
		//	return ((IDictionary<Role, IList<Permission>>)this.Permissions).ContainsKey(key);
		//}

		//bool IDictionary<Role, IList<Permission>>.Remove(Role key)
		//{
		//	return ((IDictionary<Role, IList<Permission>>)this.Permissions).Remove(key);
		//}

		//public bool TryGetValue(Role key, [MaybeNullWhen(false)] out IList<Permission> value)
		//{
		//	Boolean result = ((IDictionary<Role, IList<Permission>>)this.Permissions).TryGetValue(key, out value);

		//	SetRoles(key, value);

		//	return result;
		//}

		//public void Add(KeyValuePair<Role, IList<Permission>> item)
		//{
		//	SetRoles(item.Key, item.Value);

		//	((ICollection<KeyValuePair<Role, IList<Permission>>>)this.Permissions).Add(item);
		//}

		//public void Clear()
		//{
		//	((ICollection<KeyValuePair<Role, IList<Permission>>>)this.Permissions).Clear();
		//}

		//public bool Contains(KeyValuePair<Role, IList<Permission>> item)
		//{
		//	return ((ICollection<KeyValuePair<Role, IList<Permission>>>)this.Permissions).Contains(item);
		//}

		//public void CopyTo(KeyValuePair<Role, IList<Permission>>[] array, int arrayIndex)
		//{
		//	((ICollection<KeyValuePair<Role, IList<Permission>>>)this.Permissions).CopyTo(array, arrayIndex);
		//}

		//public bool Remove(KeyValuePair<Role, IList<Permission>> item)
		//{
		//	return ((ICollection<KeyValuePair<Role, IList<Permission>>>)this.Permissions).Remove(item);
		//}

		//public Dictionary<Role, IList<Permission>>.KeyCollection Keys
		//{
		//	get
		//	{
		//		return this.Permissions.Keys;
		//	}
		//}

		//ICollection<Role> IDictionary<Role, IList<Permission>>.Keys => ((IDictionary<Role, IList<Permission>>)this.Permissions).Keys;

		//public ICollection<IList<Permission>> Values
		//{
		//	get
		//	{
		//		return ((IDictionary<Role, IList<Permission>>)this.Permissions).Values;
		//	}
		//}

		//public int Count => ((ICollection<KeyValuePair<Role, IList<Permission>>>)this.Permissions).Count;

		//public bool IsReadOnly => ((ICollection<KeyValuePair<Role, IList<Permission>>>)this.Permissions).IsReadOnly;

		//public IList<Permission> this[Role role]
		//{
		//	get
		//	{
		//		SetRoles(role, this.Permissions[role]);

		//		return this.Permissions[role];
		//	}
		//	set
		//	{
		//		this.Permissions[role] = value;
		//	}
		//}

		//public void SetRoles(Role role, IList<Permission> values)
		//{
		//	foreach (Permission permission in values)
		//	{
		//		permission.Role = role;
		//	}
		//}
	}

}
