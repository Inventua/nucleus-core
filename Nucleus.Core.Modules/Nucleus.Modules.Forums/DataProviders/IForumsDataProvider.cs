using Nucleus.Abstractions.Models;
using Nucleus.Modules.Forums.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Forums.DataProviders
{
	public interface IForumsDataProvider : IDisposable
	{
		public Task<Forum> GetForum(Guid Id);
		public Task<IList<Forum>> ListForums(Group group);
		public Task SaveForum(Group group, Forum forum);
		public Task DeleteForum(Forum forum);


		public Task<Group> GetGroup(Guid Id);
		public Task<IList<Group>> ListGroups(PageModule pageModule);
		public Task SaveGroup(PageModule pageModule, Group group);
		public Task DeleteGroup(Group group);

		public Task<Post> GetForumPost(Guid id);
		public Task<IList<Post>> ListForumPosts(Forum forum, FlagStates approved);
		public Task SaveForumPost(Forum forum, Post post);
		public Task DeleteForumPost(Post post);
		public Task SetForumPostPinned(Post post, Boolean value);
		public Task SetForumPostLocked(Post post, Boolean value);
		public Task SetForumPostApproved(Post post, Boolean value);
		public Task SetForumPostStatus(Post post, Nucleus.Abstractions.Models.ListItem value);


		public Task<IList<Reply>> ListForumPostReplies(Post post, FlagStates approved);
		public Task<Reply> GetForumPostReply(Guid replyId);
		public Task SaveForumPostReply(Post post, Reply reply);
		public Task DeleteForumPostReply(Reply reply);

		public Task<List<Attachment>> ListPostAttachments(Guid postId);
		public Task<List<Attachment>> ListReplyAttachments(Guid postId, Guid replyId);

		public Task DeletePostAttachments(Post post);

		public Task DeleteReplyAttachments(Reply reply);
	}
}
