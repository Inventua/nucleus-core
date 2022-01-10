/**
 * Admin UI client-side handlers and behaviours
 * @requires nucleus-shared.js
 */

jQuery(Page).on("ready.admin", _handleContentLoaded);


function _handleContentLoaded(event, page, target, data, url, triggerEvent, status, request)
{
	var form;

	if (target === null || typeof (target) === 'undefined')
	{
		target = jQuery(document);
	}

	// Attach module editor forms and form-submit controls _PostPartialContent
	jQuery(document).off('submit.admin');
	jQuery(document).on('submit.admin', '.ModuleEditor form, .ModuleEditor form input[type="submit"], .ModuleEditor form button[type="submit"]', page.PostPartialContent);	

	// If an editor panel has been populated, hide sibling search results
	if (target.is('.EditorPanel'))
	{
		target.siblings('.SearchResults').hide();
	}

	// If the new content contains (or is, or is in) an element with class 'ModuleEditor' and ModuleEditor contains a form which does
	// not have a data-target attribute or the data-target is an empty string, set the data-target to .ModuleSettings.  This
	// allows module edit controls to correctly participate in the admin UI/partial rendering without module developers having 
	// to know anything about the admin UI implementation.
	if (target.is('.ModuleEditor') || target.parents('.ModuleEditor').length !== 0)
	{
		form = target.find('form');
	}
	else
	{
		form = target.find('.ModuleEditor form');
	}
				
	if (form.length !== 0)
	{
		if (typeof (form.attr('data-target')) ==='undefined' || form.attr('data-target') === '')
		{
			form.attr('data-target', '.ModuleSettings');
		}
	}

	// Manage the url (href) and visibility of the 'back' button (shown top-right of the admin slide-out pane).  
	if (typeof (this._previousUrl) !== 'undefined' && this._previousUrl !== null)// && jQuery(triggerEvent.target).parents('.ControlPanel').length !== 0)
	{
		jQuery('.AdminPageBack').attr('href', this._previousUrl).show();
		this._previousUrl = null;
	}
	else
	{
		jQuery('.AdminPageBack').removeAttr('href').hide();
	}

	this._previousUrl = (typeof (url) !== 'undefined') ? url : null;
	//if (typeof (url) !== 'undefined')
	//{
	//	this._previousUrl = url;
	//}
	//else
	//{
	//	this._previousUrl = null;
	//}

	// For clicked items within a list, apply the 'selected' css class to the selected LI, and remove from other LI elements in the parent list
	if (typeof (triggerEvent) !== 'undefined')
	{
		jQuery(triggerEvent.currentTarget).parents('ul, ol').find('LI.selected').removeClass('selected');
		if (jQuery(triggerEvent.currentTarget).is('li'))
		{
			jQuery(triggerEvent.currentTarget).addClass('selected');
		}
		else
		{
			jQuery(triggerEvent.currentTarget).parents('li').addClass('selected');
		}

		if (triggerEvent.currentTarget.scrollIntoView)
		{
			triggerEvent.currentTarget.scrollIntoView({ behavior: 'smooth' });
		}
	}

	target.find('.CloseResults').on('click', function (event) { jQuery(this).parents('.Results').hide();});

	// initialize plugins
	target.find('.HtmlEditorControl').HtmlEditor();	
	target.find('.ToggleSwitch').ToggleSwitch();
	
}
