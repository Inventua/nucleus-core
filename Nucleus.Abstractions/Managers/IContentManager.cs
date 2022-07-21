using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Managers
{
	/// <summary>
	/// Defines the interface for the content manager.
	/// </summary>
	/// <remarks>
	/// Modules can store their content (Html, or another format) using the container manager.  For example, the TextHtml and MultiContent
	/// modules use the content manager to save and retrieve their content, instead of using their own database tables.
	/// Get an instance of this class from dependency injection by including a parameter in your class constructor.
	/// </remarks>
	public interface IContentManager
	{
		/// <summary>
		/// List all content for a module.
		/// </summary>
		/// <param name="pageModule"></param>
		/// <returns></returns>
		public Task<List<Content>> List(PageModule pageModule);

		/// <summary>
		/// Get content specified by <paramref name="id"/>.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Task<Content> Get(Guid id);

		/// <summary>
		/// Save the specified content.
		/// </summary>
		/// <param name="pageModule"></param>
		/// <param name="content"></param>
		/// <returns></returns>
		public Task Save(PageModule pageModule, Content content);

		/// <summary>
		/// Delete the specified content.
		/// </summary>
		/// <param name="content"></param>
		/// <returns></returns>
		public Task Delete(Content content);

		/// <summary>
		/// Update the <see cref="Content.SortOrder"/> of the content specifed by id by swapping it with the next-highest <see cref="Content.SortOrder"/>.
		/// </summary>
		/// <param name="module"></param>
		/// <param name="contentId"></param>
		public Task MoveDown(PageModule module, Guid contentId);

		/// <summary>
		/// Update the <see cref="Content.SortOrder"/> of the content specifed by id by swapping it with the previous <see cref="Content.SortOrder"/>.
		/// </summary>
		/// <param name="module"></param>
		/// <param name="contentId"></param>
		public Task MoveUp(PageModule module, Guid contentId);
	}
}
