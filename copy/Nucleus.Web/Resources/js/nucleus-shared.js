var Page = new _Page();

function _Page()
{
	// Public methods
	this.PostPartialContent = _postPartialContent;
	this.GetPartialContent = _getPartialContent;
	this.LoadPartialContent = _loadPartialContent;
	this.GetCookie = _getCookie;
	
	// Attach Nucleus-standard event handlers when document is ready
	jQuery(document).ready(function ()
	{
		// Attach the click event for any element with a data-target attribute to _GetPartialContent
		jQuery(document).on('click', '[data-target]:not(form, input, button):not([data-method="POST"])', _getPartialContent);

		// Attach forms and form-submit controls with a data-target attribute to _PostPartialContent
		jQuery(document).on('submit', 'form[data-target], form:has(input[type="submit"][data-target], input[type="file"][data-target], button[type="submit"][data-target])', _postPartialContent);
		
		// Attach links which target an IFRAME
		jQuery(document).on('click', 'A[data-frametarget]', _loadIFrame);

		// Attach hyperlinks with a data-target, but not a data-method to _getPartialContent
		jQuery(document).on('click', 'A[data-target]:not([data-method])', _getPartialContent);

		// Attach hyperlinks with a data-method="POST" attribute to _PostPartialContent
		jQuery(document).on('click', 'A[data-method="POST"]', _postPartialContent);

		jQuery(document).on('click change keydown', 'INPUT[data-autopostbackevent], SELECT[data-autopostbackevent]', _handleAutoPostBack);

		// stop clicks inside a popup menu from being propagated further up the DOM
		jQuery(document).on('click', '.PopupMenu', function ()
		{
			event.stopPropagation();
		});

		jQuery('.dropdown-menu a.dropdown-toggle').on('click', function (e)
		{
			if (!jQuery(this).next().hasClass('show'))
			{
				jQuery(this).parents('.dropdown-menu').first().find('.show').removeClass('show');
			}
			var subMenu = jQuery(this).next('.dropdown-menu');
			/*subMenu.toggleClass('show');*/


			jQuery(this).parents('li.nav-item.dropdown.show').on('hidden.bs.dropdown', function (e)
			{
				jQuery('.dropdown-submenu .show').removeClass('show');
			});


			return false;
		});

		// handle file uploads
		jQuery(document).on('change', 'input[type=file]', function ()
		{
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

			if (jQuery(this).attr('action') !== '')
			{
				form.attr('action', jQuery(this).attr('action'));
			}
			form.submit();

			event.preventDefault();
			event.stopImmediatePropagation();
		});

		// Close popups when a user clicks outside a popup.  Click events will be propagated up to BODY if they are not handled elsewhere and 
		// prevented from bubbling with event.stopPropagation()
		jQuery('body').on('click', function ()
		{
			jQuery('.PopupMenu').fadeOut();
		});

		jQuery(Page).trigger("ready", [Page]);
	});

	function _handleAutoPostBack(event)
	{
		if (jQuery(this).attr('data-autopostbackevent') === event.type)
		{
			jQuery(this).parents('form').submit();
		}
	}

	function _loadIFrame(event)
	{
		var url;
		var targetFrameSelector;
		var targetFrame;

		if (event.currentTarget.tagName.toLowerCase() === 'a')
		{
			form = jQuery(event.currentTarget).parents('form');

			url = jQuery(event.currentTarget).attr('href');
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
			if (targetFrame.attr('data-overlay') !== '')
			{
				jQuery(targetFrame.attr('data-overlay')).show();
			}

			targetFrame.attr('src', url);
		}

		event.preventDefault();
		event.stopImmediatePropagation();
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
					_render(target, data, url, event, status, request);
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
			if (!_confirm(jQuery(event.originalEvent.submitter).attr('data-confirm'), function () { action.call(this); }))
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

		event.preventDefault();
		event.stopImmediatePropagation();
	}

	/**
	 * @summary	
	 * Extract the url and target element from a clicked element, send a GET request to the Url and render the
	 * results inside the target.
	 * 
	 * @param {any} event
	 */
	function _loadPartialContent(url, target)
	{
		if (typeof (target) === 'string')
		{
			target = jQuery(target);
		}

		jQuery.ajax({
			url: url,
			headers: { 'Accept': 'application/json, */*' },
			success: function (data, status, request) { _render(target, data, url, event, status, request); },
			error: function (request, status, message) { _handleError(target, url, event, status, message, request); }
		});

		event.preventDefault();
		event.stopImmediatePropagation();
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
		var url = jQuery(event.currentTarget).attr('href');
		var targetSelector = jQuery(event.currentTarget).attr('data-target');
		var target;

		// first, try to get the closest (single) element matching the target selector
		if (event.currentTarget !== null)
		{
			// check children
			target = jQuery(event.currentTarget).find(targetSelector).first();

			if (typeof(target) === 'undefined' || target === null || target.length === 0)
			{
				// check (self) and parents
				target = jQuery(event.currentTarget).closest(targetSelector).first();
			}
		}			

		// If that doesn't find anything, look through the whole DOM
		if (typeof (target) === 'undefined' || target === null || target.length === 0)
		{
			target = jQuery(targetSelector).first();
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
				success: function (data, status, request) { _render(target, data, url, event, status, request); },
				error: function (request, status, message) { _handleError(target, url, event, status, message, request); }
			});
		}

		event.preventDefault();
		event.stopImmediatePropagation();
	}

	function _confirm(message, action)
	{
		_dialog('Confirm', message, 'Ok', 'Cancel', action);
		return true;
	}

	function _handleError(target, url, event, status, message, request)
	{
		// Handle response code "redirect", but with header X-Location rather than location.  Used by modules which need to return a redirect (302) without the browser automatically following it.
		if (request.status === 302)
		{
			window.location.assign(request.getResponseHeader('X-Location'));
			return;
		}

		var errorData;
		var errorTag;

		if (typeof (request.responseJSON) !== 'undefined')
		{
			if (request.status === 400)
			{
				var validationMessage = '';
				// bad request.  Look for ModelState errors				
				for (var prop in request.responseJSON)
				{
					if (validationMessage !== '')
					{
						validationMessage += ', ';
					}

					jQuery('[name="' + prop + '"]').addClass('ValidationError');
					validationMessage += request.responseJSON[prop];
				}

				errorData = new Object();
				errorData.Title = 'Validation Error';
				errorData.Message = validationMessage;
				errorData.StatusCode = request.status;
				errorData.TraceId = '';
			}
			else
			{
				errorData = request.responseJSON;
			}
		}

		// catch-all for non JSON & unrecognized error results
		if (typeof (errorData) === 'undefined' || errorData === null)
		{
			errorData = new Object();
			errorData.Title = 'Error' + ' [' + request.statusText + ']';
			errorData.Message = request.responseText;
			errorData.StatusCode = request.status;
			errorData.TraceId = '';
		}

		_dialog(errorData.Title, errorData.Message)
	}

	function _dialog(title, message, okCaption, cancelCaption, action)
	{
		var DIALOG_ID = 'nucleus-dialog';

		jQuery('#' + DIALOG_ID).remove();

		if (typeof (okCaption) === 'undefined') okCaption = 'Ok';
		if (typeof (cancelCaption) === 'undefined') cancelCaption = 'Close';

		var okButton = '';
		if (typeof(action) !== 'undefined')
		{			
			okButton = '<button type="button" class="btn btn-primary" data-bs-dismiss="modal">' + okCaption +'</button>';
		}

		var dialogMarkup = jQuery(
			'<div class="modal fade" id="' + DIALOG_ID + '" tabindex="-1" aria-label="' + title + '">' +
			'  <div class="modal-dialog modal-dialog-centered modal-lg">' +
			'    <div class="modal-content">' +
			'      <div class="modal-header">' +
			'        <h5 class="modal-title">' + title + '</h5>' +
			'        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="' + cancelCaption + '"></button>' +
			'      </div>' +
			'      <div class="modal-body">' + message + '</div>' +
			'      <div class="modal-footer">' +
			okButton + 
			'        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">' + cancelCaption +'</button>' +
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

	function _render(target, data, url, event, status, request)
	{
		_preRender(target, data, status, request);

		if (typeof (request.responseJSON) !== 'undefined' && typeof (request.responseJSON.Message) !== 'undefined')
		{
			_dialog(request.responseJSON.Title, request.responseJSON.Message);			
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
				target.empty();
				target.html(data);
				target.attr('data-src', url);
			}
		}

		_postRender(target, data, status, request);
		jQuery(Page).trigger("ready", [Page, target, data, url, event, status, request]);		
		_initializeControls(target, data, status, request);
	}

	function _preRender(target, data, status, request)
	{
		// retain tab control selection.  index() is zero based, but we use a nth-child selector to set it after rendering, which is 1 based, so
		// we add one to index().
		this._selectedTab = target.find('.nav .nav-item .active').parent().index() + 1;		
	}

	function _postRender(target, data, status, request)
	{
		var overlay = jQuery(target.attr('data-overlay'));

		if (overlay.length === 0)
		{
			// target doesn't have a data-overlay attribute, try finding a parent element overlay
			overlay = jQuery(target.parents('[data-overlay]').attr('data-overlay'));
		}

		if (!target.is(':visible') && data !== '')
		{
			target.show();
		}

		if (!overlay.is(':visible') && data !== '')
		{
			overlay.show();
		}

		if (target.is(':visible') && data === '')
		{
			target.hide();
			overlay.hide();

			if (!target.hasClass('EditorPopup'))
			{
				target.parents('.EditorPopup').hide();
			}
		}

		// if we get an empty response, and are in the main window, and the target is nothing, refresh the current page
		if (self === top && data === '' && target.length === 0)
		{
			window.location.reload(true);
			return;
		}

		// if we get an empty response, and the target is in an iframe and the iFrame is not #AdminFrame and isn't an EditorPopup, hide the iframe and refresh the page
		if (self !== top && data === '' && jQuery(frameElement).attr('id') !== 'AdminFrame' && !target.hasClass('EditorPopup'))
		{
			jQuery(frameElement).hide();
			window.parent.document.dispatchEvent(new CustomEvent('Refresh'));
			return;
		}

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
}