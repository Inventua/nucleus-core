/**
 * Admin UI client-side handlers and behaviours
 * @requires nucleus-shared.js
 */

jQuery(Page).on("ready.admin", _handleContentLoaded);

var navstack = [];

function _handleContentLoaded(event, page, target, data, url, triggerEvent, status, request)
{
	var form;

	if (target === null || typeof (target) === 'undefined')
	{
		target = jQuery(document);
	}

	if (data === '' && target.parents('.nucleus-control-panel-content').length !== 0)
	{
		// if we received an empty response, close the slide-out
		window.parent.document.dispatchEvent(new CustomEvent('ExpandAdminFrame', { detail: { expand: false } }));
		jQuery('.nucleus-control-panel-content').removeClass('show');
	}

	// Attach module editor forms and form-submit controls _PostPartialContent
	jQuery(document).off('submit.admin');
	jQuery(document).on('submit.admin', '.ModuleEditor form, .ModuleEditor form input[type="submit"], .ModuleEditor form button[type="submit"]', page.PostPartialContent);	

	// If an editor panel has been populated, hide search results
	if (target.is('.nucleus-editor-panel'))
	{
		jQuery('.nucleus-search-results-wrapper').hide();
	}

	// If the new content contains a form which which does not have a data-target attribute or the data-target is an empty string, set the data-target to form.parent().  This
	// allows module edit controls to correctly participate in the admin UI/partial rendering without module developers having to know anything about the admin UI implementation.
	form = target.find('form');
				
	if (form.length !== 0)
	{
		if (typeof (form.attr('data-target')) ==='undefined' || form.attr('data-target') === '')
		{
			form.attr('data-target', 'form.parent()');
		}
	}

	// Open/close (expand) the admin frame when the user clicks an icon
	jQuery('.nucleus-control-panel .nucleus-control-panel-sidebar LI:not(#nucleus-edit-content-btn)').off('click.sidebar');
	jQuery('.nucleus-control-panel .nucleus-control-panel-sidebar LI:not(#nucleus-edit-content-btn)').on('click.sidebar', AdminMenuItem_Click);

	// Close the admin frame when the user clicks the close button at the top-right
	jQuery('.nucleus-btn-close-frame').on('click', function () { ShowAdminFrame(false, ''); return false; });

	// Edit button handler (toggle edit mode)
	jQuery('#nucleus-edit-content-btn').on('click', ToggleEditMode);
	
	function AdminMenuItem_Click(event)
	{
		var url = jQuery(event.currentTarget).find('A').attr('href');

		if (jQuery('.nucleus-adminpage').data("src") === url && navstack[navstack.length-1] === url)
		{
			ShowAdminFrame(false, '');
			// Return false to prevent content load when we are closing the admin frame
			return false;
		}
		else
		{
			ShowAdminFrame(true, url);
			// Don't return false, we want the event handler in nucleus-shared.js to execute after this
		}
	}

	function ShowAdminFrame(show, url)
	{
		window.parent.document.dispatchEvent(new CustomEvent('ExpandAdminFrame', { detail: { expand: show } }));
		jQuery('.nucleus-adminpage').data('src', url);

		if (show)
		{
			jQuery('.nucleus-control-panel-content').addClass('show');
		}
		else 
		{
			jQuery('.nucleus-control-panel-content').removeClass('show');
			Reload();
		}
	}

	function ToggleEditMode(e)
	{
		Reload(!Page.GetCookie('nucleus_editmode'));
		return false;
	}

	function Reload(setEditMode)
	{
		window.parent.document.dispatchEvent(new CustomEvent('Refresh', { detail: { setEditMode: setEditMode } }));
	}

	jQuery('.nucleus-btn-page-back').off('click.managenav');
	jQuery('.nucleus-btn-page-back').on('click.managenav', function ()
	{
		navstack.pop();		
	});

	if (navstack.length !== 0 && navstack[navstack.length - 1] !== url)
	{
		jQuery('.nucleus-btn-page-back').attr('href', navstack[navstack.length-1]).show();
	}
	else
	{
		jQuery('.nucleus-btn-page-back').removeAttr('href').hide();
	}

	if (typeof (url) !== 'undefined' && navstack[navstack.length - 1] !== url)
	{
		navstack.push(url);
	}

	// For clicked items within a list, apply the 'selected' css class to the selected LI, and remove from other LI elements in the parent list
	if (typeof (triggerEvent) !== 'undefined' && triggerEvent !== null)
	{
		jQuery(triggerEvent.currentTarget).parents('ul, ol').find('LI.selected').removeClass('selected');
		if (jQuery(triggerEvent.currentTarget).is('li'))
		{
			jQuery(triggerEvent.currentTarget).addClass('selected');
		}
		else
		{
			jQuery(triggerEvent.currentTarget).parent('li').addClass('selected');
		}

		if (triggerEvent.currentTarget.scrollIntoView)
		{
			triggerEvent.currentTarget.scrollIntoView({ behavior: 'smooth' });
		}
	}

	// Handler for the search results 'close' button
	target.find('.nucleus-btn-close-results').on('click', function (event) { jQuery(this).parents('.nucleus-search-results').hide();});

	// initialize plugins
	if (jQuery().HtmlEditor) { target.find('.HtmlEditorControl').HtmlEditor(); }
	if (jQuery().ToggleSwitch) { target.find('.ToggleSwitch').ToggleSwitch(); }
	if (jQuery().PageList) { target.find('.nucleus-page-list').PageList(); }
	
}
