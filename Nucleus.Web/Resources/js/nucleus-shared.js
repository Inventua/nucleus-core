var Page = new _Page();

function _Page()
{
	// Public methods
	this.PostPartialContent = _postPartialContent;
	this.GetPartialContent = _getPartialContent;
	this.LoadPartialContent = _loadPartialContent;
	this.GetCookie = _getCookie;
	this.AddEventHandlers = _addEventHandlers;

	// Attach Nucleus-standard event handlers when document is ready
	jQuery(document).ready(function ()
	{
		// Capture the user's timezone.  We have to * -1 because the javascript getTimezoneOffset function returns an "opposite" value (The number of minutes returned 
		// by getTimezoneOffset()	is positive if the local time zone is behind UTC, and negative if the local time zone is ahead of UTC.For example, for UTC + 10, -600 will be returned.)
		var timezoneOffset = new Date().getTimezoneOffset() * -1;
		document.cookie = 'timezone-offset=' + timezoneOffset.toString() + '; path=/;SameSite=Strict;max-age=3600';

		// a elements (links) with a data-target which exist after initial page rendering (not dynamically populated) are tracked by the Google Analytics javascript, which
		// intermittently causes page navigation (to the url in href) *after* _getPartialContent has executed.  We move the href value to data-href (which is handled by _getPartialContent)
		// to work around this issue.
		jQuery('a[data-target][href], a[data-frametarget][href]').each(function (index, element)
		{
			jQuery(element).attr('data-href', jQuery(element).attr('href'));
			jQuery(element).attr('href', '#0;');
		});

		// Attach the click event for any element with a data-target attribute to _getPartialContent
		jQuery(document).on('click', '[data-target]:not(form, input, button):not([data-method="POST"])', _getPartialContent);

		// Attach forms and form-submit controls with a data-target attribute to _PostPartialContent
		jQuery(document).on('submit', 'form[data-target], form:has(input[type="submit"][data-target], input[type="file"][data-target], button[type="submit"][data-target])', _postPartialContent);
				
		// Attach links which target an IFRAME
		jQuery(document).on('click', 'a[data-frametarget], button[data-frametarget]', _loadIFrame);

		// Attach hyperlinks with a data-target, but not a data-method to _getPartialContent
		// Removed: this duplicates the binding above ([data-target]:not(form, input, button):not([data-method="POST"]))
		//jQuery(document).on('click', 'a[data-target]:not([data-method])', _getPartialContent);
				
		// Attach hyperlinks with a data-method="POST" attribute to _PostPartialContent
		jQuery(document).on('click', 'a[data-method="POST"]', _postPartialContent);

		// Automatically POST the form containing the specified control when the specified event is triggered
		jQuery(document).on('click change keydown keyup input', 'INPUT[data-autopostbackevent], SELECT[data-autopostbackevent]', _handleAutoPostBack);

		// Handle ENTER key 
		jQuery(document).on('keydown', 'input', function (event)
		{
			if (event.key === "Enter")
			{
				// Search the form for a button to click.  
				var form = jQuery(this).parents('form');

				if (form.length !== 0)
				{
					// If there is one (and only one) button with class "nucleus-default-button", click it.
					var defaultButton = form.find('.nucleus-default-button');
					if (defaultButton.length === 1)
					{
						defaultButton.click();
						event.preventDefault();
						return;
					}
					// If there is one (and only one) button with class "btn-primary", click it.
					var primaryButton = form.find('.btn-primary');
					if (primaryButton.length === 1)
					{
						primaryButton.click();
						event.preventDefault();
						return;
					}
					// If there is one (and only one) input[type=submit], click it.
					var inputSubmit = form.find('input[type="submit"]');
					if (inputSubmit.length === 1)
					{
						inputSubmit.click();
						event.preventDefault();
						return;
					}
				}
			}
		});

		/* menu-submenu handling */

		/* Make left/right keys work the same as up/down */
		jQuery('.nucleus-menu .dropdown-menu').on('keydown', function (e)
		{
			if ([37, 39].includes(e.which))  // left arrow, right arrow
			{
				var getToggleButton = jQuery(this).siblings('[data-bs-toggle="dropdown"]:not(.disabled):not(:disabled)');

				if (getToggleButton.length === 0) return;
				var instance = bootstrap.Dropdown.getOrCreateInstance(getToggleButton);

				if (e.which === 37)  // left arrow
				{
					e.key = 'ArrowUp';
				}

				if (e.which == 39)  // right arrow
				{
					e.key = 'ArrowDown';
				}

				event.stopPropagation();
				instance.show();
				instance._selectMenuItem(e);
				return;
			}
		});

		/* Show submenus on (sub)menu item mouse click */
		jQuery('.dropdown-submenu .dropdown-toggle').on('click', function (e)
		{
			var subMenu = jQuery(this).next('.dropdown-menu');
			
			if (!jQuery(this).next().hasClass('show'))
			{
				jQuery('.dropdown-menu').not(subMenu.parents()).removeClass('show');
			}

			subMenu.on('shown.bs.collapse', function ()
			{
				subMenu.css('margin-left', '');
				var overflowpx = jQuery(window).width() - (subMenu.offset().left + subMenu.outerWidth(true));

				if (overflowpx < 0)
				{
					subMenu.css('margin-left', overflowpx);
				}
			})

			subMenu.collapse('show');

			return false;
		});

		/* Close all submenus when mouse is clicked elsewhere */
		jQuery(document).on('click', '', function ()
		{
			jQuery('.dropdown-submenu .dropdown-menu').removeClass('show');
		});

		// handle file uploads
		jQuery(document).on('change', 'input[type=file]', function ()
		{
			event.preventDefault();
			event.stopImmediatePropagation();

			var form = jQuery(this).parents('form');
			var parent = jQuery(this).parents('.FileSelector');

			parent.find('.UploadProgress').siblings().hide();
			parent.find('.UploadProgress').show();

			form.on('progress', function (e, percent)
			{
				parent.find('.UploadProgress .progress-bar').attr('area-valuenow', percent.toString());
				parent.find('.UploadProgress .progress-bar').css('width', percent.toString() + '%');
				parent.find('.UploadProgress .progress-bar').text(percent.toString() + '%');
			});

			form.on('error', function ()
			{
				parent.find('.UploadProgress').hide();
				parent.find('.UploadProgress').siblings().show();
			});

			form.attr('action', jQuery(this).attr('formaction'));
			form.submit();			
		});

		// Close popups when a user clicks outside a popup.  Click events will be propagated up to BODY if they are not handled elsewhere and 
		// prevented from bubbling with event.stopPropagation()
		jQuery('body').on('click', function ()
		{
			jQuery('.PopupMenu').fadeOut();
		});

		jQuery(Page).trigger("ready", [{ page: Page }]);
	});

	function _handleAutoPostBack(event)
	{
		if (jQuery(this).attr('data-autopostbackevent') === event.type)
		{
			event.preventDefault();
			event.stopImmediatePropagation();

			jQuery(this).parents('form').submit();			
		}
	}

	function _loadIFrame(event)
	{
		event.preventDefault();
		event.stopImmediatePropagation();

		var url;
		var targetFrameSelector;
		var targetFrame;

		if (event.currentTarget.tagName.toLowerCase() === 'a')
		{
			form = jQuery(event.currentTarget).parents('form');

			if (jQuery(event.currentTarget).attr('href') !== undefined)
			{
				url = jQuery(event.currentTarget).attr('href');
			}
			if (jQuery(event.currentTarget).attr('data-href') !== undefined)
			{
				url = jQuery(event.currentTarget).attr('data-href');
			}
			targetFrameSelector = jQuery(event.currentTarget).attr('data-frametarget');
		}
		else if (event.currentTarget.tagName.toLowerCase() === 'button')
		{
			form = jQuery(event.currentTarget).parents('form');

			url = jQuery(event.currentTarget).attr('formaction');
			targetFrameSelector = jQuery(event.currentTarget).attr('data-frametarget');
		}

		if (typeof (event.originalEvent) !== 'undefined' && typeof (event.originalEvent.submitter) !== 'undefined')
		{
			var newFrameTarget = jQuery(event.originalEvent.submitter).attr('data-frametarget');
			if (typeof (newFrameTarget) !== 'undefined' && newFrameTarget !== '')
			{
				targetFrameSelector = newFrameTarget;
			}
		}

		targetFrame = jQuery(targetFrameSelector);
		if (targetFrame.length !== 0)
		{
			targetFrame.attr('src', url);
		}
	}

	function _getTarget(eventTarget, targetSelector)
	{
		var originalSelector = targetSelector;

		if (originalSelector === 'window') return jQuery(window);
		if (originalSelector === 'document') return jQuery(document);

		if (originalSelector.endsWith('.parent()'))
		{
			targetSelector = targetSelector.substring(0, targetSelector.lastIndexOf('.parent()'));
		}

		// first, try to get the closest (single) element matching the target selector
		if (eventTarget !== null && eventTarget.length > 0)
		{
			// check children
			target = eventTarget.find(targetSelector).first();

			if (target === null || target.length === 0)
			{
				// check (self) and parents
				target = eventTarget.closest(targetSelector).first();
			}
		}

		// If that doesn't find anything, look through the whole DOM
		if (target === null || target.length === 0)
		{
			target = jQuery(targetSelector).first();
		}

		if (originalSelector.endsWith('.parent()'))
		{
			target = target.parent();
		}

		// If the target is a modal, then the target that we really mean is the modal's body
		if (target.is('.modal'))
		{
			target = target.find('.modal-body');
		}

		return target;
	}

	/**
	 * @summary	
	 * Extract the url and target element from a form, send a POST request to the Url and render the
	 * results inside the target.
	 * 
	 * @param {any} event
	 */
	function _postPartialContent(event)
	{
		event.preventDefault();
		event.stopImmediatePropagation();

		var form;
		var url;
		var targetSelector;
		var target;
		var eventTarget;

		if (event.currentTarget.tagName.toLowerCase() === 'form')
		{
			form = jQuery(event.currentTarget);
			eventTarget = form;
			url = form.attr('action');
			targetSelector = form.attr('data-target');
		}
		else if (event.currentTarget.tagName.toLowerCase() === 'a')
		{
			form = jQuery(event.currentTarget).parents('form');
			eventTarget = jQuery(event.currentTarget);

			url = jQuery(event.currentTarget).attr('href');
			targetSelector = jQuery(event.currentTarget).attr('data-target');

			if (typeof (url) === 'undefined')
				url = form.attr('action');

			if (typeof (targetSelector) === 'undefined')
				targetSelector = form.attr('data-target');
		}

		if (typeof (event.originalEvent) !== 'undefined' && typeof (event.originalEvent.submitter) !== 'undefined')
		{
			var formAction = jQuery(event.originalEvent.submitter).attr('formaction');
			if (typeof (formAction) !== 'undefined' && formAction !== '')
			{
				url = formAction;
				eventTarget = jQuery(event.originalEvent.submitter);
			}

			var newTarget = jQuery(event.originalEvent.submitter).attr('data-target');
			if (typeof (newTarget) !== 'undefined' && newTarget !== '')
			{
				targetSelector = newTarget;
				eventTarget = jQuery(event.originalEvent.submitter);
			}
		}

		target = _getTarget(eventTarget, targetSelector);

		// reset validation error highlighting
		target.find('.ValidationError').removeClass('ValidationError');

		var action = function ()
		{
			jQuery.ajax({
				url: url,
				method: 'POST',
				enctype: form.attr('enctype'),
				data: (form.attr('enctype') === 'multipart/form-data') ? new FormData(form[0]) : form.serialize(),
				headers: { 'Accept': 'application/json, */*' },
				xhr: function ()
				{
					var xhr = new window.XMLHttpRequest();
					//Download progress
					xhr.upload.addEventListener("progress", function (evt)
					{
						if (evt.lengthComputable)
						{
							var percentComplete = Math.round((evt.loaded / evt.total) * 100);
							form.trigger('progress', percentComplete);
						}
					}, false);
					return xhr;
				},
				processData: (form.attr('enctype') === 'multipart/form-data') ? false : true,
				contentType: (form.attr('enctype') === 'multipart/form-data') ? false : 'application/x-www-form-urlencoded; charset=UTF-8',
				success: function (data, status, request)
				{
					_render(target, eventTarget, data, url, event, status, request);
					form.trigger('success', [target, data, url, event, status, request]);
				},
				error: function (request, status, message)
				{
					_handleError(target, url, event, status, message, request);
					form.trigger('error', [target, url, event, status, message, request]);
				}
			})
		};

		if (typeof (event.originalEvent) !== 'undefined' && typeof (event.originalEvent.submitter) !== 'undefined' && typeof (jQuery(event.originalEvent.submitter).attr('data-confirm')) !== 'undefined')
		{
			// submit form after confirmation
			if (!_confirm(jQuery(event.originalEvent.submitter).attr('data-confirm'), jQuery(event.originalEvent.submitter).attr('data-confirm-title'), function () { action.call(this); }))
			{
				// confirm function returned false to indicate that dialog plugin is not available, submit form with no confirmation
				action.call(this);
			};
		}
		else
		{
			// submit form with no confirmation
			action.call(this);
		}
	}

	/**
	 * @summary	
	 * Extract the url and target element from a clicked element, send a GET request to the Url and render the
	 * results inside the target.
	 * 
	 * @param {any} event
	 */
	function _loadPartialContent(event, url, target)
	{
		event.preventDefault();
		event.stopImmediatePropagation();

		if (typeof (target) === 'string')
		{
			target = jQuery(target);
		}

		jQuery.ajax({
			url: url,
			headers: { 'Accept': 'application/json, */*' },
			success: function (data, status, request) { _render(target, null, data, url, event, status, request); },
			error: function (request, status, message) { _handleError(target, url, event, status, message, request); }
		});
	}

	/**
	 * @summary	
	 * Extract the url and target element from a clicked element, send a GET request to the Url and render the
	 * results inside the target.
	 * 
	 * @param {any} event
	 */
	function _getPartialContent(event)
	{
		event.preventDefault();
		event.stopPropagation();
		
		var url;
		var targetSelector;
		var target;

		if (jQuery(event.currentTarget).attr('data-target') !== undefined)
		{
			if (jQuery(event.currentTarget).attr('href') !== undefined)
			{
				url = jQuery(event.currentTarget).attr('href');
			}
			if (jQuery(event.currentTarget).attr('data-href') !== undefined)
			{
				url = jQuery(event.currentTarget).attr('data-href');
			}
			targetSelector = jQuery(event.currentTarget).attr('data-target');
			target = _getTarget(jQuery(event.currentTarget), targetSelector);
		}
		else if (jQuery(event.target).attr('data-target') !== undefined)
		{
			if (jQuery(event.target).attr('href') !== undefined)
			{
				url = jQuery(event.target).attr('href');
			}
			if (jQuery(event.target).attr('data-href') !== undefined)
			{
				url = jQuery(event.target).attr('data-href');
			}
			targetSelector = jQuery(event.target).attr('data-target');
			target = _getTarget(jQuery(event.target), targetSelector);
		}

		// reset validation error highlighting
		target.find('.ValidationError').removeClass('ValidationError');

		/// In some cases, the item with a data-target will contain an element with a href (for example when a LI 
		/// contains an A element).  Get the Url from the first child with a href attribute in these cases.
		if (typeof url === 'undefined')
		{
			url = jQuery(event.currentTarget).find('[href]').attr('href');
		}

		if (typeof (target) !== 'undefined' && typeof (url) !== 'undefined')
		{
			jQuery.ajax({
				url: url,
				headers: { 'Accept': 'application/json, */*' },
				success: function (data, status, request) { _render(target, jQuery(event.currentTarget), data, url, event, status, request); },
				error: function (request, status, message) { _handleError(target, url, event, status, message, request); }
			});
		}

		return false;
	}

	function _confirm(message, title, action)
	{
		if (typeof (title) === 'undefined' || title === '')
		{
			title = 'Confirm';
		}
		_dialog(title, message, 'question', 'Ok', 'Cancel', action);
		return true;
	}

	function _handleError(target, url, event, status, message, request)
	{
		// Handle response code "redirect", but with header X-Location rather than location.  Used by modules which need to return a redirect (302) without the browser automatically following it.
		if (request.status === 302)
		{
			if (request.getResponseHeader('X-Location-Target') === '_top')
			{
				window.parent.document.dispatchEvent(new CustomEvent('Open', { detail: { target: request.getResponseHeader('X-Location') } }));
			}
			else
			{
				window.location.assign(request.getResponseHeader('X-Location'));
			}
			return;
		}

		var errorData;
		var errorTag;

		if (typeof (request.responseJSON) !== 'undefined')
		{
			if (request.status === 400)
			{
				// bad request.  Parse ModelState errors		
				var messages = new Array(request.responseJSON.length);
				var elementSelector ='';

				// get a list of elements with validation errors
				for (var prop in request.responseJSON)
				{
					var element = jQuery('[name="' + prop + '"]');
					element.addClass('ValidationError');

					if (elementSelector !== '') elementSelector += ',';
					elementSelector += '#' + element.attr('id');
				}

				var elements = jQuery(elementSelector);

				// sort the messages by element ordinal position
				for (var prop in request.responseJSON)
				{
					var element = jQuery('[name="' + prop + '"]');
					var index = _getElementIndex(elements, element.attr('id'));
					if (index !== undefined)
					{
						message = request.responseJSON[prop].toString();
						if (!message.endsWith("."))
						{
							message += '.';
						}
						messages[index] = '<li>' + message + '</li>';
					}
        }

				var validationMessage = '<ul>' + messages.join('') + '</ul>';		

				errorData = new Object();
				errorData.title = 'Validation Error';
				errorData.detail = validationMessage;
				errorData.statusCode = request.status;
			}
			else
			{
				errorData = request.responseJSON;
			}
		}
		else
		{			
			if (request.status === 403)
			{
				errorData = new Object();
				errorData.title = 'Access Denied';
				errorData.detail = request.responseText === null || request.responseText === '' ? 'Your account does not have permission to access this function.' : request.responseText;
				errorData.statusCode = request.status;
			}
		}

		// catch-all for non JSON & unrecognized error results
		if (typeof (errorData) === 'undefined' || errorData === null)
		{
			errorData = new Object();
			errorData.title = 'Error' + ' [' + request.statusText + ']';
			errorData.detail = request.responseText;
			errorData.statusCode = request.status;
		}

		_dialog(errorData.title, errorData.detail, 'error')
	}

	function _getElementIndex(items, id)
	{
		for (count = 0; count < items.length; count++)
		{
			if (jQuery(items[count]).attr('id') === id) return count;
    }
	}

	function _dialog(title, message, icon, okCaption, cancelCaption, action)
	{
		var DIALOG_ID = 'nucleus-dialog';

		jQuery('#' + DIALOG_ID).remove();

		if (typeof (okCaption) === 'undefined') okCaption = 'Ok';
		if (typeof (cancelCaption) === 'undefined') cancelCaption = 'Close';

		var okButton = '';
		if (typeof (action) !== 'undefined')
		{
			okButton = '<button type="button" class="btn btn-primary" data-bs-dismiss="modal">' + okCaption + '</button>';
		}

		var iconElement = '';
		var iconClass = '';
		if (typeof (icon) !== 'undefined')
		{
			switch (icon)
			{
				case 'question':
					iconClass = ' icon-question';
					iconElement = '<div class="dialog-icon"><span class="nucleus-material-icon">&#xe887;</span></div>';
					break;
				case 'warning':
					iconClass = ' icon-warning';
					iconElement = '<div class="dialog-icon"><span class="nucleus-material-icon">&#xe002;</span></div>';
					break;
				case 'error':
					iconClass = ' icon-error';
					iconElement = '<div class="dialog-icon"><span class="nucleus-material-icon">&#xe000;</span></div>';
					break;
			}
		}

		// convert array of messages into a list
		if (Array.isArray(message))
		{
			var messages = new Array(message.length);
			for (index = 0; index < message.length; index++)
			{
				messageItem = message[index].toString();
				if (!messageItem.endsWith("."))
				{
					messageItem += '.';
				}
				messages[index] = '<li>' + messageItem + '</li>';
			}
			message = '<ul>' + messages.join('') + '</ul>';
		}

		var dialogMarkup = jQuery(
			'<div class="modal fade' + iconClass + '" id="' + DIALOG_ID + '" tabindex="-1" aria-label="' + title + '">' +
			'  <div class="modal-dialog modal-dialog-centered modal-lg">' +
			'    <div class="modal-content">' +
			'      <div class="modal-header">' +
			'        <h5 class="modal-title">' + title + '</h5>' +
			'        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="' + cancelCaption + '"></button>' +
			'      </div>' +
			'      <div class="modal-body">' +
			iconElement +
			'        <div class="dialog-message">' + message.replace(new RegExp('\n', 'g'), '<br/>') + '</div>' +
			'      </div>' +
			'      <div class="modal-footer">' +
			okButton +
			'        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">' + cancelCaption + '</button>' +
			'      </div>' +
			'    </div>' +
			'  </div >' +
			'</div>');

		jQuery('body').append(dialogMarkup);

		if (typeof (action) !== 'undefined')
		{
			dialogMarkup.find('.btn-primary').on('click', action);
		}

		jQuery('#' + DIALOG_ID).on('hidden.bs.modal', function () { jQuery('#' + DIALOG_ID).remove(); });
		var modal = new bootstrap.Modal('#' + DIALOG_ID);
		modal.show();
	}

	function _render(target, source, data, url, event, status, request)
	{
		_preRender(target, source, data, status, request);

		if (typeof (request.responseJSON) !== 'undefined' && typeof (request.responseJSON.message) !== 'undefined')
		{
			_dialog(request.responseJSON.title, request.responseJSON.message);
		}
		else
		{
			// response is HTML content

			// If the content is a full HTML page, ignore target.  This is normally when a "302 redirect" is returned.  Browsers automatically
			// follow the redirect so we don't have a way to know that it was a redirect.
			if (data.startsWith('<!DOCTYPE'))
			{
				document.open();
				document.write(data);
				document.close();
			}
			else
			{
				// keep passwords 
				var passwords = new Array();
				target.find('input[type=password]').each(function (index, value)
				{
					var element = jQuery(value);
					passwords[index] = new Object({ name: element.prop('name'), value: element.val() });
				});

				target.empty().html(data).attr('data-src', url);

				// restore passwords
				jQuery(passwords).each(function (index, value)
				{
					target.find('input[type=password][name="' + passwords[index].name + '"]').val(passwords[index].value);
				});

				passwords = null;
			}
		}

		_postRender(target, source, data, status, request);
		jQuery(Page).trigger("ready", [{ page: Page, target: target, data: data, url: url, event: event, status: status, request: request }]);
		_initializeControls(target, data, status, request);
	}

	function _preRender(target, source, data, status, request)
	{
		// retain tab control selection.  index() is zero based, but we use a nth-child selector to set it after rendering, which is 1 based, so
		// we add one to index().
		this._selectedTab = target.find('.nav .nav-item .active').parent().index() + 1;

		// if the original element is in a modal, and the target is not inside the same modal, close the modal 
		if (source !== null && source.parents('.modal').length !== 0)
		{
			if (source.parents('.modal').find(target).length === 0)
			{
				source.parents('.modal').modal('hide');
			}
		}
	}

	function _postRender(target, source, data, status, request)
	{
		if (!target.is(':visible') && data !== '')
		{
			target.show();
		}

		if (!target.is('.modal, .modal-body') && data !== '' && jQuery('.modal').is(':visible'))
		{
			// if the source element is in a modal, and the target is not inside the same modal, close the modal
			if (source !== null && source.parents('.modal').length !== 0)
			{
				if (source.parents('.modal').find(target).length === 0)
				{
					// a modal is visible, data is non-blank, target is not the modal or one of its descendants, hide the modal
					jQuery('.modal:visible').modal('hide');					
				}
			}
		}

		if (target.parents('.modal').length !== 0)
		{
			// target is in a modal.  If content was returned, check/set the modal caption, set any empty data-targets.  If content is 
			// empty, close the modal.
			var wrapper = target.parents('.modal');
			var settingsDialog;

			if (data !== '')
			{
				var heading = jQuery('.nucleus-modal-caption').first();
				if (heading.length !== 0)
				{
					var title = wrapper.find('.modal-title');
					title.html(heading.html());
					heading.remove();
				}

				// Set the target of forms, buttons to the modal's body, if the control doesn't already have a data-target and the control's form doesn't already have a data-target.
				jQuery(target).find('form, button, input').each(function (index, element)
				{
					element = jQuery(element);

					if (element.tagName !== 'form')
					{
						form = element.parents('form');
					}

					if
						(
							(typeof (element.attr('data-target')) === 'undefined' || element.attr('data-target') === '')
							&&
							(typeof(form) !== 'undefined' && (typeof (form.attr('data-target')) === 'undefined' || form.attr('data-target') === ''))
						)
					{
						element.attr('data-target', '.modal-body');
					}
				});

				// if there are any existing modal backdrops (overlays), remove them so that they don't get drawn on top of 
				// one another after a postback & increase the opacity of the backdrop.
				jQuery('.modal-backdrop').remove();

				var modal = new bootstrap.Modal(wrapper, { backdrop: 'static' });
				modal.show();

				wrapper.on('hidden.bs.modal', function () { jQuery('.modal-backdrop').remove(); });

			}
			else
			{
				wrapper.modal('hide');
			}

		}

		// if we get an empty response, and are in the main window, and the target is nothing, refresh the current page
		if (self === top && data === '' && target.length === 0)
		{
			window.location.reload(true);
			return;
		}

		// if we get an empty response, and the target is in an iframe and the iFrame is not #AdminFrame, hide the iframe and refresh the page
		if (self !== top && data === '' && (jQuery(frameElement).attr('id') !== 'AdminFrame' || source.hasClass('nucleus-dialogresult')))
		{
			jQuery(frameElement).hide();
			window.parent.document.dispatchEvent(new CustomEvent('Refresh'));
			return;
		}

		// attempt to set focus to the first control in the response

		// prefer an INPUT/SELECT/TEXTAREA
		var focusableElement = target.find('.nucleus-default-control, input:not([type="hidden"]), select, textarea').first();
		if (focusableElement.length === 0)
		{
			// set focus to another element type if no INPUT/SELECT/TEXTAREA was found
			focusableElement = target.find('button, a, [tabindex]:not([tabindex="-1"])').first();
		}
		focusableElement.first().focus();

		// adjust dates from UTC to local
		_setupDateFields(target);
	}

	function _setupDateFields(target)
	{
		// .net model binding serializes DateTimeOffset to a format which datetime-local fields don't understand, so we have to set 
		// a custom attribute to the required value & convert it here.  
		target.find('input[type=datetime-local]').each(function (index, ui)
		{
			var element = jQuery(ui);
			var value = element.attr('data-value');

			if (typeof (value) !== 'undefined' && value !== '')
			{
				// using .toISOString gets us the right format for datetime-local, but outputs the date in UTC time (we want local time), so we
				// work around by subtracting the local timezone offset first
				var offset_ms = new Date().getTimezoneOffset() * 60000;
				var newValue = new Date(new Date(value).getTime() - offset_ms);
				if (newValue !== 'Invalid Date')
				{
					element.val(newValue.toISOString().slice(0, 16));
				}
			}
		});
	}

	function _initializeControls(target, data, status, request)
	{
		if (typeof (this._selectedTab) !== 'undefined' && this._selectedTab !== -1)
		{
			// trigger click to select the tab (and un-select other tabs)
			target.find('.nav .nav-item:nth-child(' + this._selectedTab + ') button').trigger('click');
		}
	}

	function _getCookie(name)
	{
		function escape(s) { return s.replace(/([.*+?\^$(){}|\[\]\/\\])/g, '\\$1'); }
		var match = document.cookie.match(RegExp('(?:^|;\\s*)' + escape(name) + '=([^;]*)'));
		return match ? match[1] : null;
	}

	// Add _Layout.cshtml event handlers, which are used to communicate/execute events from the admin iframe to the main window.

	// ExpandAdminFrame:  Show or hide the admin iframe
	function _addEventHandlers(toggleEditCookieName)
	{
		window.document.addEventListener('ExpandAdminFrame', function (args)
		{
			var adminFrame = jQuery('#AdminFrame');
			args.detail !== null && args.detail.expand ? adminFrame.addClass('Expanded') : adminFrame.removeClass('Expanded');
		}, false);

		// Refresh event. Set or clear the edit-mode cookie, fade out the display and reload the current Url
		window.document.addEventListener('Refresh',	function (args)
		{			
			if (args.detail !== null && typeof (args.detail.setEditMode) !== 'undefined')
			{
				var cookieValue = args.detail.setEditMode ? 'true' : '';
				var cookieAge = args.detail.setEditMode ? '3600' : '0';
				document.cookie = toggleEditCookieName + '=' + cookieValue + '; Path=/; max-age=' + cookieAge;
			}

			jQuery('body').fadeTo('opacity', '0.3');
			window.location.reload(true);
		}, false);

		// Open event.  Navigate to the Url specified by args.detail.target
		window.document.addEventListener('Open', function (args)
		{
			if (args.detail !== null && typeof (args.detail.target) !== 'undefined')
			{
				jQuery('body').fadeTo('opacity', '0.3');
				window.location = args.detail.target;
			}
		}, false);


	// Initialize a popup iframe (from _PopupEditor.cshtml) by finding it's parent .modal, and creating a Bootstrap modal.
	window.document.addEventListener('InitFrame',
		function (args)
		{
			if (args.detail !== null && typeof (args.detail.element) !== 'undefined')
			{
				// find the modal which contains the args.detail.element DOM element (the iframe)
				var element = jQuery(args.detail.element);
				if (!element.is('iframe')) return;
				var wrapper = element.parents('.modal');

				// Set modal's title to the iframe title
				var titleElement = wrapper.find('.modal-title');
				titleElement.html(args.detail.element.title);

				if (!wrapper.is(':visible'))
				{
					// Create the modal
					var newDialog = new bootstrap.Modal(wrapper, { backdrop: 'static' });
					// when the modal containing the popup dialog is hidden, fade it out for .3 seconds and (at the same time) reload the page
					wrapper.on('hidden.bs.modal', function () { jQuery('body').fadeTo('opacity', '0.3'); window.location.reload(true); });
					// show the modal
					newDialog.show();
				}
				// make sure the iframe is visible
				element.show();
			}
		}, false);
	}
}