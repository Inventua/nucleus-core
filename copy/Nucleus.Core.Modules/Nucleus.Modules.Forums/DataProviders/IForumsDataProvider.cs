using Nucleus.Abstractions.Models;
using Nucleus.Modules.Forums.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Forums.DataProviders
{
	public interface IForumsDataProvider : IDisposable, Nucleus.Core.DataProviders.Abstractions.IDataProvider
	{
		public Forum GetForum(Guid Id);
		public Guid GetForumGroupId(Forum forum);
		public IList<Forum> ListForums(Group group);
		public void SaveForum(Group group, Forum forum);
		public void DeleteForum(Forum forum);


		public Group GetGroup(Guid Id);
		public IList<Group> ListGroups(PageModule pageModule);
		public void SaveGroup(PageModule pageModule, Group group);
		public void DeleteGroup(Group group);

		public Post GetForumPost(Guid id);
		public IList<Post> ListForumPosts(Forum forum, FlagStates approved);
		public void SaveForumPost(Forum forum, Post post);
		public void DeleteForumPost(Post post);
		public void SetForumPostPinned(Post post, Boolean value);
		public void SetForumPostLocked(Post post, Boolean value);
		public void SetForumPostApproved(Post post, Boolean value);
		public void SetForumPostStatus(Post post, Nucleus.Abstractions.Models.ListItem value);


		public IList<Reply> ListForumPostReplies(Post post, FlagStates approved);
		public Reply GetForumPostReply(Guid replyId);
		public void SaveForumPostReply(Post post, Reply reply);
		public void DeleteForumPostReply(Reply reply);

		public List<Attachment> ListPostAttachments(Guid postId);
		public List<Attachment> ListReplyAttachments(Guid postId, Guid replyId);
	}
}
