/*! nucleus-shared | Nucleus shared client-side handlers and behaviours | (c) Inventua Pty Ptd.  www.nucleus-cms.com */
var Page = new _Page();
function _Page()
{
  // public methods
  this.PostPartialContent = _postPartialContent;
  this.GetPartialContent = _getPartialContent;
  this.LoadPartialContent = _loadPartialContent;
  this.GetCookie = _getCookie;
  this.AddEventHandlers = _addEventHandlers;
  this.AttachCopyButton = _attachCopyButton;
  this.CopyToClipboard = _copyToClipboard;
  this.Dialog = _dialog;
  this.EnableEnhancedToolTips = _enableEnhancedToolTips;
  this.ClosePopupDialog = _closePopupDialog;

  // constants
  var DIALOG_ID = 'nucleus-dialog';

  // private properties
  var _progressTimeoutId = -1;

  // attach Nucleus-standard event handlers when document is ready
  jQuery(document).ready(function ()
  {
    // capture the user's timezone.  We have to * -1 because the javascript getTimezoneOffset function returns an "opposite" value (The number of minutes returned 
    // by getTimezoneOffset()	is positive if the local time zone is behind UTC, and negative if the local time zone is ahead of UTC.For example, for UTC + 10, -600 will be returned.)
    var timezoneOffset = new Date().getTimezoneOffset() * -1;
    document.cookie = 'timezone-offset=' + timezoneOffset.toString() + '; path=/;SameSite=Strict;max-age=3600';

    _restoreScrollPostition();
    _trackScrollPosition();

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
    jQuery(document).on('click', 'button[type="button"][data-target][data-href]', _getPartialContent);

    // Attach forms with a data-target attribute and forms which contain form-submit controls that have a data-target attribute to _PostPartialContent
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

    // Handle popstate event.  When the data-useurl attribute is added to an element which causes partial rendering (or to its form element), we 
    // use the browser history API to push the url specified in data-useurl to the browser address bar.
    jQuery(window).on("popstate", function (event)
    {
      if (history.state && typeof history.state.url !== 'undefined')
      {
        window.location = history.state.url;
      }
    });

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

    /* modal maximize/minimize */
    jQuery(document).on('dblclick', '.modal-dialog .modal-header', function (event)
    {
      _maximizeDialog(this, !(jQuery(this).parents('.modal-dialog').first().hasClass('modal-full-size')));
    });

    jQuery(document).on('click', '.modal-dialog .modal-header .btn-maximize', function (event)
    {
      _maximizeDialog(this, true);
    });

    jQuery(document).on('click', '.modal-dialog .modal-header .btn-normalsize, .modal-dialog .modal-header .btn-close', function (event)
    {
      _maximizeDialog(this, false);
    });

    jQuery(document).on('click', '.nucleus-settings-frame *[data-bs-dismiss="modal"]', function (event)
    {
      _closePopupDialog(this);
    });

    /* menu-submenu handling */

    /* 
      Handle keyboard events (arrow keys) on menu items which are links to a page but which which also have children.  These have
      a toggle button immediately following them, which is what we use to call _selectMenuItem in response to key presses.
    */
    jQuery('.nucleus-menu [data-bs-toggle="dropdown-keyboardonly"]').on('keydown', function (event)
    {
      if ([37, 38, 39, 40].includes(event.which))  // arrow keys: 37,38,39,40: left/up/right/down
      {
        var menuToggleButton = jQuery(this).siblings('[data-bs-toggle="dropdown"]:not(.disabled):not(:disabled)');
        if (menuToggleButton.length > 0)
        {
          var instance = bootstrap.Dropdown.getOrCreateInstance(menuToggleButton);
          event.key = [37, 38].includes(e.which) ? 'ArrowUp' : 'ArrowDown';

          event.stopPropagation();
          instance.show();
          instance._selectMenuItem(event);
        }
      }
    });

    /* Make left/right keys work the same as up/down */
    jQuery('.nucleus-menu .dropdown-menu').on('keydown', function (event)
    {
      if ([37, 39].includes(event.which))  // left arrow, right arrow
      {
        var menuToggleButton = jQuery(this).siblings('[data-bs-toggle="dropdown"]:not(.disabled):not(:disabled)');

        if (menuToggleButton.length > 0)
        {
          var instance = bootstrap.Dropdown.getOrCreateInstance(menuToggleButton);
          event.key = event.which === 37 ? 'ArrowUp' : 'ArrowDown';

          event.stopPropagation();
          instance.show();
          instance._selectMenuItem(event);
        }
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
    jQuery(document).on('change', 'input[type=file]', function (event)
    {
      event.preventDefault();
      event.stopImmediatePropagation();

      var form = jQuery(this).parents('form');
      var uploadprogressWrapper = form.find('.UploadProgress').first();
      var uploadprogress = uploadprogressWrapper.find('progress');
      var uploadprogressLabel = uploadprogressWrapper.find('label');

      uploadprogressWrapper.siblings().css('opacity', '.4');
      uploadprogressWrapper.siblings('.alert').hide();
      uploadprogressWrapper.show();

      form.on('progress', function (event, percent)
      {
        uploadprogress.attr('value', percent.toString());
        uploadprogress.text(percent.toString() + '%');

        var filesCount = 0;
        var labeltext = 'file';

        jQuery(event.currentTarget).find('input[type=file]').each(function (index, element)
        {
          if (element != null && typeof (element.files) !== 'undefined')
          {
            filesCount += element.files.length;
          }
        });

        if (filesCount > 1)
        {
          labeltext = 'files';
        }

        if (percent < 100)
        {
          uploadprogressLabel.text('Uploading ' + labeltext + ' ...');
        }
        else
        {
          uploadprogressLabel.text('Upload Complete.  Processing ' + labeltext + ' ...');
        }
      });

      form.on('error', function ()
      {
        uploadprogressWrapper.hide();
        uploadprogressWrapper.siblings().css('opacity', '1');
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

    jQuery('.nucleus-default-control').focus();
    jQuery(Page).trigger("ready", [{ page: Page }]);
    _initializeControls(jQuery(document), null, null, null, null);
  });

  function _handleAutoPostBack(event)
  {
    if (jQuery(this).attr('data-autopostbackevent') === event.type)
    {
      event.preventDefault();
      event.stopImmediatePropagation();

      var newEvent = jQuery.Event('submit', { originalEvent: event });
      jQuery(this).parents('form').first().trigger(newEvent);

      //jQuery(this).parents('form').submit();
    }
  }

  function _buildScrollPositionKey(element)
  {
    var scrollTrackId = (element === document) ? "document" : jQuery(element).attr('data-track-scroll');
    return 'scrolltop:' + document.location.pathname + ':' + scrollTrackId;
  }

  function _isSessionStorageAvailable()
  {
    try
    {
      return typeof (window['sessionStorage']) !== 'undefined';
    }
    catch (e)
    {
      return false;
    }
  }

  function _trackScrollPosition()
  {
    if (!_isSessionStorageAvailable() || window.self !== window.top) return;
    jQuery(window).on('beforeunload', function (event)
    {
      jQuery('document, [data-track-scroll]').each(function (index, element)
      {
        try
        {
          sessionStorage.setItem(_buildScrollPositionKey(element), jQuery(element).scrollTop().toString());
        }
        catch (ex) { } // suppress error
      });
    });
  }

  function _restoreScrollPostition()
  {
    if (!_isSessionStorageAvailable() || window.self !== window.top) return;
    jQuery('document, [data-track-scroll]').each(function (index, element)
    {
      var key = _buildScrollPositionKey(element);
      try
      {
        if (sessionStorage[key])
        {
          jQuery(element).scrollTop(sessionStorage.getItem(key));
        }
      }
      catch (ex) { } // suppress error 
    });
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

  function _getUseUrl(event)
  {
    var useUrl;

    if (typeof event !== 'undefined' && event !== null && typeof event.originalEvent !== 'undefined')
    {
      if (typeof event.originalEvent !== 'undefined' && event.originalEvent !== null && typeof event.originalEvent.submitter !== ' undefined' && event.originalEvent.submitter !== null && jQuery(event.originalEvent.submitter).is('[data-useurl]')) 
      {
        useUrl = jQuery(event.originalEvent.submitter).attr('data-useurl');
      }
      else if (event.currentTarget !== 'undefined' && event.currentTarget !== null && jQuery(event.currentTarget).is('[data-useurl]'))
      {
        useUrl = jQuery(event.currentTarget).attr('data-useurl');
      }
    }
    return useUrl;
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

    if (typeof targetSelector === 'undefined')
    {
      return;
    }

    target = _getTarget(eventTarget, targetSelector);

    // reset validation error highlighting
    target.find('.ValidationError').removeClass('ValidationError');

    if (targetSelector === 'window')
    {
      return;
    }

    event.preventDefault();
    event.stopImmediatePropagation();

    var action = function ()
    {
      _indicateProgress.call(this, target, url, event);

      jQuery.ajax({
        url: url,
        async: true,
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
          _handleError(target, eventTarget, url, event, status, message, request);
          form.trigger('error', [target, url, event, status, message, request]);
        }
      })
    };

    if (typeof (event.originalEvent) !== 'undefined' && typeof (event.originalEvent.submitter) !== 'undefined' && typeof (jQuery(event.originalEvent.submitter).attr('data-confirm')) !== 'undefined')
    {
      // submit form after confirmation
      if (!_confirm(jQuery(event.originalEvent.submitter).attr('data-confirm'), jQuery(event.originalEvent.submitter).attr('data-confirm-title'), function ()
      {
        // User clicked ok.
        jQuery(event.originalEvent.submitter).trigger('confirmed', [target, url, event]);
        action.call(this);
      }))
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
    var source = null;

    if (event !== null)
    {
      if (event.currentTarget !== null && typeof event.currentTarget !== 'undefined')
      {
        source = jQuery(event.currentTarget);
      }

      event.preventDefault();
      event.stopImmediatePropagation();
    }

    if (typeof (target) === 'string')
    {
      target = jQuery(target);
    }

    _indicateProgress.call(this, target, url, event);

    jQuery.ajax({
      url: url,
      async: true,
      headers: { 'Accept': 'application/json, */*' },
      success: function (data, status, request) { _render(target, source, data, url, event, status, request); },
      error: function (request, status, message) { _handleError(target, source, url, event, status, message, request); }
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
      _indicateProgress.call(this, target, url, event);

      jQuery.ajax({
        url: url,
        async: true,
        headers: { 'Accept': 'application/json, */*' },
        success: function (data, status, request) { _render(target, jQuery(event.currentTarget), data, url, event, status, request); },
        error: function (request, status, message) { _handleError(target, jQuery(event.currentTarget), url, event, status, message, request); }
      });
    }

    return false;
  }

  /**
   * @summary	
   * Render a progress indicator for the control which triggered the specified event.
   * 
   * @param {any} event
   */
  function _indicateProgress(target, url, event)
  {
    // send a progress event so that progress indicators can be handled externally
    jQuery(Page).trigger("progress", [{ page: Page, target: target, url: url, event: event }]);

    // figure out which control initiated the event
    var triggerControl = jQuery(this);

    if (event !== null)
    {
      if (event.originalEvent !== null && typeof event.originalEvent !== 'undefined')
      {
        if (event.originalEvent.submitter !== null && typeof event.originalEvent.submitter !== 'undefined')
        {
          triggerControl = jQuery(event.originalEvent.submitter);
        }
        else if (event.originalEvent.target !== null && typeof event.originalEvent.target !== 'undefined')
        {
          triggerControl = jQuery(event.originalEvent.target);
        }
      }
    }

    // if the trigger control does not have a nucleus-show-progress class, look for ancestors of the element which do have the nucleus-show-progress class
    if (triggerControl != null && !triggerControl.hasClass('nucleus-show-progress'))
    {
      triggerControl = triggerControl.parents('.nucleus-show-progress').first();
    }

    if (triggerControl != null && triggerControl.hasClass('nucleus-show-progress'))
    {
      _cancelProgressIndicator();
      _progressTimeoutId = window.setTimeout(() =>
      {
        var progress = jQuery('<div class="spinner-border spinner-border-sm text-primary nucleus-progress-spinner ms-2" role="status"/>');

        if (triggerControl.hasClass('nucleus-show-progress-inside'))
        {
          progress.appendTo(triggerControl);
        }
        else if (triggerControl.hasClass('nucleus-show-progress-after'))
        {
          progress.insertAfter(triggerControl);
        }
        else if (triggerControl.hasClass('nucleus-show-progress-before'))
        {
          progress.insertBefore(triggerControl);
        }
      }, 500);
    }
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

  function _handleError(target, source, url, event, status, message, request)
  {
    // Handle response code "redirect", but with header X-Location rather than location.  Used by modules which need to return a redirect (302) 
    // without the browser automatically following it.
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
        // bad request.  Can be a ModelState JSON response, or an error message
        if (typeof request.responseJSON.title !== 'undefined' && typeof request.responseJSON.detail !== 'undefined')
        {
          errorData = request.responseJSON;
        }
        else
        {
          // Parse ModelState errors		
          var messages = new Array(request.responseJSON.length);
          var elementSelector = '';

          // get a list of elements with validation errors
          for (var prop in request.responseJSON)
          {
            var element = jQuery('[name="' + prop + '"]');
            if (!element.is('.HtmlEditorControl'))
            {
              element.addClass('ValidationError');
            }
            else
            {
              element.parents('.settings-control').first().addClass('ValidationError');
            }

            if (elementSelector !== '') elementSelector += ',';
            elementSelector += '#' + element.attr('id');
          }

          var elements = jQuery(elementSelector);

          // sort the messages by element ordinal position
          for (var prop in request.responseJSON)
          {
            var element = jQuery('[name="' + prop + '"]');
            var index = _getElementIndex(elements, element.attr('id'));
            if (typeof (index) === 'undefined')
            {
              // id doesn't match any on-screen element, so add it to the end of the list
              index = messages.length;
            }
            message = request.responseJSON[prop].toString();
            if (!message.endsWith("."))
            {
              message += '.';
            }
            messages[index] = '<li>' + message + '</li>';
          }

          // if the first element is in a tab, select the tab
          if (elements.length !== 0)
          {
            var selectedTab = elements.first().parents('.tab-pane');            
            if (selectedTab.length !== 0)
            {
              var selectedTabIndex = selectedTab.index() + 1
              // trigger click to select the tab (and un-select other tabs)
              selectedTab.parents('.tab-content').siblings('nav').find('.nav-item:nth-child(' + selectedTabIndex + ') button').trigger('click');              
            }
          }

          // display error message
          var validationMessage = '<ul>' + messages.join('') + '</ul>';

          errorData = new Object();
          errorData.title = 'Validation Error';
          errorData.detail = validationMessage;
          errorData.statusCode = request.status;
          errorData.icon = "warning";
        }
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
      errorData.detail = request.responseText ?? '';
      errorData.statusCode = request.status;
    }

    _cancelProgressIndicator();
    _removeProgressIndicator(source);
    _dialog(errorData.title, errorData.detail, errorData.icon ?? 'error')
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
    if (typeof (okCaption) === 'undefined') okCaption = 'Ok';
    if (typeof (cancelCaption) === 'undefined') cancelCaption = 'Close';

    var okButton = '';
    if (typeof action !== 'undefined')
    {
      okButton = '<button type="button" class="btn btn-primary" data-bs-dismiss="modal">' + okCaption + '</button>';
    }

    var iconElement = '';
    var iconClass = '';
    if (typeof icon !== 'undefined')
    {
      switch (icon.toLowerCase())
      {
        case 'question':
          iconClass = ' icon-question';
          iconElement = '<div class="dialog-icon"><span class="nucleus-material-icon">&#xe887;</span></div>';
          break;
        case 'warning':
          iconClass = ' icon-warning';
          iconElement = '<div class="dialog-icon"><span class="nucleus-material-icon">&#xe002;</span></div>';
          break;
        case 'alert':
          iconClass = ' icon-alert';
          iconElement = '<div class="dialog-icon"><span class="nucleus-material-icon">&#xe7f4;</span></div>';
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

    message = message.replace(new RegExp('\n', 'g'), '<br/>');

    // this code handles cases where a dialog has already been displayed, by replacing the title and content of
    // an existing dialog, if a dialog is already present.  This typicalically happens when a user clicks a "submit"
    // control to execute an action a second time, while a request is already in progress, like clicking the "Login"
    // button more than once, so the title and message is usually the same as what is already being displayed.
    var modalInstance = bootstrap.Modal.getInstance(document.getElementById('nucleus-dialog'));
    var modalContent =
      '    <div class="modal-content">' +
      '      <div class="modal-header">' +
      '        <h5 class="modal-title">' + title + '</h5>' +
      '        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="' + cancelCaption + '"></button>' +
      '      </div>' +
      '      <div class="modal-body flex-row">' +
      iconElement +
      '        <div class="dialog-message">' + message + '</div>' +
      '      </div>' +
      '      <div class="modal-footer">' +
      okButton +
      '        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">' + cancelCaption + '</button>' +
      '      </div>' +
      '    </div>'

    if (modalInstance === null)
    {
      // new dialog, create new dialog
      var dialogMarkup = jQuery(
        '<div class="modal fade' + iconClass + '" id="' + DIALOG_ID + '" tabindex="-1" aria-label="' + title + '">' +
        '  <div class="modal-dialog modal-dialog-centered modal-lg">' +
        modalContent +
        '  </div >' +
        '</div>');

      jQuery('body').append(dialogMarkup);
    }
    else
    {
      // existing dialog, replace content
      jQuery('#' + DIALOG_ID).find('.modal-dialog').html(modalContent);
    }

    if (typeof action !== 'undefined')
    {
      jQuery('#' + DIALOG_ID).find('.btn-primary').on('click', action);
    }

    // only create a new instance of bootstrap.Modal for the dialog if one doesn't already exist.  
    if (modalInstance === null)
    {
      // when a dialog is closed, remove it from the DOM
      jQuery('#' + DIALOG_ID).off('hidden.bs.modal');
      jQuery('#' + DIALOG_ID).on('hidden.bs.modal', function () { jQuery('#' + DIALOG_ID).remove(); });

      // create a new instance of bootstrap.Modal for the dialog
      modalInstance = new bootstrap.Modal('#' + DIALOG_ID);
    }

    modalInstance.show();
  }

  function _render(target, source, data, url, event, status, request)
  {
    _preRender(target, source, data, url, status, request);

    var useUrl = _getUseUrl(event);
    if (typeof useUrl !== 'undefined' && useUrl !== null)
    {
      var state =
      {
        url: useUrl
      };
      window.history.pushState(state, '', useUrl);
    }

    if (typeof (request.responseJSON) !== 'undefined' && typeof (request.responseJSON.message) !== 'undefined')
    {
      _cancelProgressIndicator();
      _removeProgressIndicator(source);
      _dialog(request.responseJSON.title, request.responseJSON.message, request.responseJSON.icon);
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

        // write new content
        target
          .empty()
          .html(data)
          .attr('data-src', url);

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
    _initializeControls(target, data, url, status, request);
  }

  function _preRender(target, source, data, url, status, request)
  {
    // retain tab control selection.  index() is zero based, but we use a nth-child selector to set it after rendering, which is 1 based, so
    // we add one to index().
    this._selectedTab = target.find('.nav .nav-item .active').parent().index() + 1;

    if (typeof this._selectedPagePath !== 'undefined')
    {
      try
      {
        var urlPath = this._selectedPagePath.startsWith('/') ? this._selectedPagePath.substring(1) : this._selectedPagePath;  
        this._selectedTabPath = new URL(document.baseURI + urlPath).pathname;
      }
      catch (err)
      {
        console.error(err);
      }
    }
    this._selectedPagePath = url;

    // if the original element is in a modal, and the target is not inside the same modal, close the modal 
    if (source !== null && source.parents('.modal').length !== 0)
    {
      if (source.parents('.modal').first().find(target).length === 0)
      {
        _maximizeDialog(source.parents('.modal').first(), false);
        source.parents('.modal').first().modal('hide');
      }
    }
  }

  function _cancelProgressIndicator()
  {
    // cancel setTimeout() handles used to create progress indicators.
    if (_progressTimeoutId >= 0)
    {
      window.clearTimeout(_progressTimeoutId);
      _progressTimeoutId = -1;
    }
  }

  function _postRender(target, source, data, status, request)
  {
    _cancelProgressIndicator();
    _removeProgressIndicator(source);

    if (!target.is(':visible') && data !== '')
    {
      target.show();
    }

    if (!target.is('.modal, .modal-body') && data !== '' && jQuery('.modal').is(':visible'))
    {
      // if the source element is in a modal, and the target is not inside the same modal, close the modal
      if (source !== null && source.parents('.modal').length !== 0)
      {
        if (source.parents('.modal').first().find(target).length === 0)
        {
          // a modal is visible, data is non-blank, target is not the modal or one of its descendants, hide the modal
          jQuery('.modal:visible').first().modal('hide');
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
            (typeof (form) !== 'undefined' && (typeof (form.attr('data-target')) === 'undefined' || form.attr('data-target') === ''))
          )
          {
            element.attr('data-target', '.modal-body');
          }
        });

        if (!wrapper.is(':visible'))
        {
          var modal = new bootstrap.Modal(wrapper, { backdrop: 'static' });
          modal.show();
        }

        wrapper.off('shown.bs.modal');
        wrapper.on('shown.bs.modal', function (event) { _removeDuplicateOverlays(event); });
        wrapper.off('hidden.bs.modal');
        wrapper.on('hidden.bs.modal', function (event) { _removeRelatedOverlays(event, this); });
      }
      else
      {
        _maximizeDialog(wrapper, false);
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

    // set the text and disabled attribute for <option> elements with a text value of "-".  We need to do this in .setTimeout to give the 
    // content to render first.
    jQuery('select option').each(function ()
    {
      if (jQuery(this).text() == '-')
      {
        window.setTimeout(function (element) 
        {
          var parent = element.parent();
          // determine how many dashes will fit
          var measure = jQuery('<span>-</span>');
          measure.css('font-size', element.parent().css('font-size'));
          parent.parent().append(measure);
          var dashCount = Math.floor(parent.width() / measure.width());
          measure.remove();
          // set option element to disabled, fill with the calculated number of dashes
          if (!Number.isInteger(dashCount)) dashCount = 10;
          element.text("-".repeat(dashCount));
          element.attr('disabled', 'disabled');
        }, 100, jQuery(this));
      }
    });

    // attempt to set focus to the first control in the response

    // if the currently-selected control is an INPUT[type=search], leave focus where it is
    if (!jQuery(document.activeElement).is('input[type=search]'))
    {
      // prefer an INPUT/SELECT/TEXTAREA
      var focusableElement = target.find('.nucleus-default-control, input:not([type="hidden"]), select, textarea').first();
      if (focusableElement.length === 0)
      {
        // set focus to another element type if no INPUT/SELECT/TEXTAREA was found
        focusableElement = target.find('button, a, [tabindex]:not([tabindex="-1"])').first();
      }

      if (focusableElement.length > 0)
      {
        window.setTimeout(function ()
        {
          // only focus on the "found" element if it is in view to prevent scrolling.  We do this in a .setTimeout because elements in 
          // modals aren't immediately visible & elements can't receive focus until they are visible.
          if (_isInView(focusableElement.first()))
          {
            focusableElement.first().focus();
          }
        }, 100);
      }
    }
  }

  // remove any progress indicators for the specified event source element.
  function _removeProgressIndicator(source)
  {
    if (typeof source !== 'undefined' && source !== null && source.hasClass('nucleus-show-progress'))
    {
      if (source.hasClass('nucleus-show-progress-before') || source.hasClass('nucleus-show-progress-after'))
      {
        source.siblings('.nucleus-progress-spinner').remove();
      }
      else
      {
        source.find('.nucleus-progress-spinner').remove();
      }
    }
  }

  // Remove duplicate .modal-backdrop elements with the same parent.  When we do a partial render and replace/re-open a modal, 
  // Bootstrap doesn't know about it, so it renders extra overlays - this code removes all duplicate overlays with the same parent, 
  // leaving only one behind.
  function _removeDuplicateOverlays(event)
  {
    jQuery.uniqueSort(jQuery('.modal-backdrop').parent()).each(function (index, parentElement)
    {
      jQuery(parentElement).find('.modal-backdrop:not(:first)').remove();
      event.stopPropagation();
    });
  }

  // Remove .modal-backdrop elements at the same level in the DOM as the specified element.  This is required because when we do a 
  // partial render and replace content / re-open a modal, Bootstrap loses track of the overlay which is related to a modal.
  function _removeRelatedOverlays(event, element)
  {
    jQuery(element).siblings('.modal-backdrop').remove();
    jQuery(element).parents().first().siblings('.modal-backdrop').remove();

    // If all modals have been closed then remove all backdrops.  This is to ensure that we remove any "orphaned" backdrops.
    if (jQuery('.modal:visible').length === 0)
    {
      jQuery('body').children('.modal-backdrop').remove();
    }
    event.stopPropagation();
  }

  function _isInView(element)
  {
    var elementTop = element.offset().top;
    var elementBottom = elementTop + element.outerHeight();
    var viewportTop = jQuery(window).scrollTop();
    var viewportBottom = viewportTop + jQuery(window).height();
    return elementBottom > viewportTop && elementTop < viewportBottom;
  };

  function _initializeControls(target, data, url, status, request)
  {
    if (typeof this._selectedTab !== 'undefined' && this._selectedTab !== -1 && typeof url != 'undefined' && url !== null)
    {
      try
      {
        var urlPath = url.startsWith('/') ? url.substring(1) : url;        
        var newPath = new URL(document.baseURI + urlPath).pathname;
        if (typeof this._selectedTabPath !== 'undefined' && this._selectedTabPath === newPath)
        {
          // trigger click to select the tab (and un-select other tabs)
          target.find('.nav .nav-item:nth-child(' + this._selectedTab + ') button').trigger('click');
        }
      }
      catch (err)
      {
        console.error(err);
      }
    }

    // initialize plugins
    if (jQuery().HtmlEditor) { target.find('.HtmlEditorControl').HtmlEditor({ isAdminMode: false }); }
    if (jQuery().ToggleSwitch) { target.find('.ToggleSwitch').ToggleSwitch(); }
    if (jQuery().PageList) { target.find('.nucleus-page-list').PageList(); }

    Page.EnableEnhancedToolTips(true);
  }

  function _getCookie(name)
  {
    function escape(s) { return s.replace(/([.*+?\^$(){}|\[\]\/\\])/g, '\\$1'); }
    var match = document.cookie.match(RegExp('(?:^|;\\s*)' + escape(name) + '=([^;]*)'));
    return match ? match[1] : null;
  }

  function _attachCopyButton(selector, childselector, isBlockElement)
  {
    if (typeof isBlockElement === 'undefined' || isBlockElement === null)
    {
      isBlockElement = false;
    }

    // Create copy buttons for selected elements
    jQuery(selector).each(function (index, element) 
    {
      var copyButton;
      if (isBlockElement)
      {
        copyButton = jQuery('<button class="nucleus-copy-button nucleus-material-icon btn btn-none d-block ms-auto mb-1 fs-small" type="button" title="Copy to Clipboard">&#xe14d;</button>');
      }
      else
      {
        copyButton = jQuery('<button class="nucleus-copy-button nucleus-material-icon btn btn-none ms-1 fs-small" type="button" title="Copy to Clipboard">&#xe14d;</button>');
      }

      if (typeof childselector !== 'undefined' && childselector !== null)
      {
        if (jQuery(element).find(childselector).length > 0)
        {
          copyButton[0].CopyTarget = jQuery(element).find(childselector);
        }
        else
        {
          copyButton[0].CopyTarget = jQuery(element).closest(childselector);
        }
      }
      else
      {
        copyButton[0].CopyTarget = jQuery(element);
      }

      // copy button handler
      copyButton.on('click', function ()
      {
        _copyToClipboard(this.CopyTarget);
        return false;
      });

      if (isBlockElement)
      {
        copyButton.insertBefore(jQuery(element));
      }
      else
      {
        copyButton.insertAfter(jQuery(element));
      }
    });

  }

  function _copyToClipboard(element)
  {
    if (jQuery(element).length === 0) return;
    var value;

    //var temp = jQuery("<textarea>");
    //jQuery("body").append(temp);
    //if (jQuery(element).is('input, textarea'))
    //{
    //	temp.val(jQuery(element).val().trim()).select();
    //}
    //else
    //{
    //	temp.val(jQuery(element)[0].innerText.replace(/[^\x20-\x7E\x0A\x0D]/g, '').trim()).select();
    //  }
    //document.execCommand("copy");
    //temp.remove();

    if (jQuery(element).is('input, textarea'))
    {
      value = jQuery(element).val().trim();
    }
    else
    {
      value = jQuery(element)[0].innerText.replace(/[^\x20-\x7E\x0A\x0D]/g, '').trim();
    }

    try
    {
      navigator.clipboard.writeText(value)
    }
    catch
    {
      // ignore 
    }
  }

  function _maximizeDialog(element, maximize)
  {
    var modal;
    if (jQuery(element).hasClass('.modal-dialog'))
    {
      modal = jQuery(element);
    }
    else
    {
      modal = jQuery(element).parents('.modal-dialog').first();
    }

    if (maximize)
    {
      modal.addClass('modal-full-size');
    }
    else
    {
      modal.removeClass('modal-full-size');
    }
  }

  function _closePopupDialog(source)
  {
    if (jQuery(source).parents('.modal').length !== 0)
    {
      // editor popup from index
      jQuery(source).parents('.modal').first().modal('hide');
    }
    else
    {
      // inline editor
      window.parent.document.dispatchEvent(new CustomEvent('ClosePopupDialog', {}));
    }
  }

  function _enableEnhancedToolTips(enable)
  {
    jQuery('.settings-control[title]').each(function (index, element)
    {
      if (enable)
      {
        /* We want tooltips to show when the user hovers over the caption, rather than the input controls */
        var tooltipTarget = jQuery(element).find('label span');

        /* Save the settings-control tile in data-bs-original-title and remove the title attribute, so the browser doesn't also try to show the title. */
        /* This is the same as the built-in bootstrap tooltip behavior, but we have to do it ourselves because we are targeting 'tooltipTarget' rather */
        /* than the settings-control element. */
        jQuery(element)
          .attr('data-bs-original-title', jQuery(element).attr('title'))
          .removeAttr('title', '');

        /* Create the tooltip/event handler/etc.  Note that the "title" argument is a function which gets the parent settings-control title. */
        tooltipTarget.attr('data-bs-toggle', 'tooltip');
        var instance = new bootstrap.Tooltip(tooltipTarget,
          {
            trigger: 'hover',
            placement: 'bottom',
            container: element,
            title: function () { return jQuery(this).parents('.settings-control').first().attr('data-bs-original-title') },
            delay: 600
          });

        /* Bootstrap assumes that the tooltip target is what contains the title (regardless of our 'title' function above), and tries to save  */
        /* the element's title.  This results in two empty attributes */
        tooltipTarget
          .removeAttr('data-bs-original-title')
          .removeAttr('title', '');

        element.addEventListener('shown.bs.tooltip', function ()
        {
          /*  override bootstrap/popper positioning, in order to bottom-left align the tooltip (instead of bottom-centered), but only inside 
           *  elements with .settings-control css class */
          if (jQuery(this).hasClass('settings-control') && jQuery(this).css('position') == 'relative')
          {
            // Popper will set the transform css property to matrix(1,0,0,1, x-position, y-position).  We want to remove the x-position (set it to zero
            // in order to left-align the tooltip.
            var matrixTransform = jQuery(this).find('.tooltip').css('transform');
            if (matrixTransform.includes('matrix'))
            {
              var values = matrixTransform.split(',');
              matrixTransform = values[0] + ',' + values[1] + ',' + values[2] + ',' + values[3] + ',' + '0' + ',' + values[5];
              jQuery(this).find('.tooltip').css('transform', matrixTransform);
            }

            /* position the tooltip arrow to the left */
            jQuery(this).find('.tooltip-arrow').css('transform', 'translateX(10px)');
          }
        });

        // Hide tooltip when the user clicks anywhere or presses any key
        jQuery(document).on('click keydown', function ()
        {
          jQuery('.settings-control label span[data-bs-toggle="tooltip"]').each(function (index, element)
          {
            var inst = bootstrap.Tooltip.getInstance(element);
            if (inst != null) inst.hide();
          });
        });
      }
      else
      {
        jQuery(element).attr('title', jQuery(element).attr('data-bs-original-title'));
        jQuery(element).find('label span').removeAttr('data-bs-toggle', 'tooltip');
        bootstrap.Tooltip.getInstance(jQuery(element).find('label span[data-bs-toggle="tooltip"]')).disable();
      }
    });
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
    window.document.addEventListener('Refresh', function (args)
    {
      if (args.detail !== null && typeof (args.detail.setEditMode) !== 'undefined')
      {
        var cookieValue = args.detail.setEditMode ? 'true' : '';
        var cookieAge = args.detail.setEditMode ? '3600' : '0';
        document.cookie = toggleEditCookieName + '=' + cookieValue + '; Path=/; max-age=' + cookieAge;
      }

      jQuery('body').fadeTo('opacity', '0.3');
      window.location.reload();
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
    window.document.addEventListener('InitializePopupDialog',
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

    window.document.addEventListener('ClosePopupDialog',
      function (args)
      {
        jQuery('#nucleus-popupdialog').modal('hide');
      }, false);
  }
}