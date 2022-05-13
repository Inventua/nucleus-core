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
		public Task<Nucleus.Abstractions.Models.Paging.PagedResult<Post>> ListForumPosts(Forum forum, Nucleus.Abstractions.Models.Paging.PagingSettings settings, FlagStates approved);

		public Task SaveForumPost(Forum forum, Post post);
		public Task DeleteForumPost(Post post);
		public Task SetForumPostPinned(Post post, Boolean value);
		public Task SetForumPostLocked(Post post, Boolean value);
		public Task SetForumPostApproved(Post post, Boolean value);
		public Task SetForumPostRejected(Post post, Boolean value);
		public Task SetForumPostStatus(Post post, Nucleus.Abstractions.Models.ListItem value);

		public Task<IList<Reply>> ListForumPostReplies(Post post, FlagStates approved);
		public Task<Reply> GetForumPostReply(Guid replyId);
		public Task SaveForumPostReply(Post post, Reply reply);
		public Task DeleteForumPostReply(Reply reply);
		public Task SetForumPostReplyApproved(Reply reply, Boolean value);
		public Task SetForumPostReplyRejected(Reply reply, Boolean value);

		public Task<List<Attachment>> ListPostAttachments(Guid postId);
		public Task<List<Attachment>> ListReplyAttachments(Guid postId, Guid replyId);

		public Task DeletePostAttachments(Post post);

		public Task DeleteReplyAttachments(Reply reply);

		public Task SubscribeForum(Guid forumId, Guid userId);
		public Task UnSubscribeForum(Guid forumId, Guid userId);
		public Task<List<User>> ListForumSubscribers(Guid forumId);

		public Task SubscribeForumPost(Guid postId, Guid userId);
		public Task UnSubscribeForumPost(Guid postId, Guid userId);
		public Task<List<User>> ListPostSubscribers(Guid postId);

		public Task<ForumSubscription> GetForumSubscription(Guid forumId, Guid userId);
		public Task<PostSubscription> GetPostSubscription(Guid PostId, Guid userId);

		public Task<PostTracking> GetPostTracking(Guid postId, Guid userId);
		public Task SavePostTracking(Guid postId, Guid userId);
		public Task DeletePostTracking(Guid postId, Guid userId);

		public Task<Boolean> IsQueued(MailQueue mailQueue);
		public Task SaveMailQueue(MailQueue mailQueue);
		public Task SetMailQueueStatus(MailQueue mailQueue);

		public Task DeleteMailQueue(MailQueue mailQueue);
		public Task<IList<MailQueue>> ListMailQueue();
		
		public Task TruncateMailQueue(TimeSpan sentBefore);

		public Task<UserSubscriptions> ListUserSubscriptions(Guid userId);
	}
}
