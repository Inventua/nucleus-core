/* =============================================================
 * Plug-in that creates a list of jump links based on the jQuery 
 * selector used as the root for detected elements.
 * =============================================================
 */
(function ($)
{
  jQuery.fn.PageLinkViewer = function (conf)
  {
    var options = jQuery.extend({
      operationMode: null,
      rootSelector: 'body',
      includeHeaders: '',
      headingClass: ''
    }, conf);

    // private variables
    let _autoIdCounter = 0;
    let _headerElements;

    // private functions
    function _getAllHeaders(element)
    {
      this._headerElements = element.children().find(':header');
      var previousHeaderIndex = 10;
      var listElement = jQuery('<ol></ol>'); //wrapper used to add to DOM.
      var parentElement = listElement;

      if (this._headerElements.length > 0)
      {
        jQuery.each(this._headerElements, function (index)
        {
          var headerIndex = parseInt(this.nodeName.substring(1), 10);

          if (_isHeaderValid(this))
          {
            var pageLinkItem = jQuery('<ol></ol>').append(_getPageLink(jQuery(this)));

            var parentListItem = _getParentListItem(parentElement, jQuery(this), headerIndex);
            if (parentListItem !== null)
            {
              listElement.find(parentListItem).append(pageLinkItem);
            }
            else
            {
              listElement.append(pageLinkItem);
            }
          }
          previousHeaderIndex = headerIndex;
        });
      }

      // remove the original <ol></ol> wrapper
      listElement.children('ol').children().unwrap();

      return listElement;
    }

    function _getParentListItem(listElement, element, headerLevel)
    {
      var parentListItem = null;

      if (this._headerElements[0] === element)
      {
        return null;
      }
      else
      {
        // copy the preceding elements into another array and reverse
        var reverseArray = jQuery(this._headerElements.slice(0, this._headerElements.index(jQuery(element)) + 1)).get().reverse();

        // loop through the array until we find the first parent header
        jQuery(reverseArray).each(function (index)
        {
          let headerIndex = parseInt(this.nodeName.substring(1), 10);

          if (_isHeaderValid(this))
          {
            if (headerIndex < headerLevel)
            {
              // found nearest parent
              let elementId = jQuery(this).prop('id');

              let relativeUrl = new URL(window.location.href);
              relativeUrl.hash = '#' + elementId;

              parentListItem = jQuery(listElement).closest('ol').find('a[href*="' + relativeUrl.toString() + '"]').parent();
              return false;
            }
          }
        });
      }

      return parentListItem;
    }

    function _setHeaderElementId(element)
    {
      var headerElement = jQuery(element);
      var elementId = headerElement.prop('id');

      if (elementId === '')
      {
        var elementId = headerElement
          .text()
          .replace(/\s/, '-')
          .replace(/[^A-Za-z0-9]/g, '')
          .toLowerCase();

        if (jQuery('#' + elementId).length !== 0)
        {
          do
          {
            _autoIdCounter++;
            elementId = 'pageLink' + _autoIdCounter;
          } while (jQuery('#' + elementId).length !== 0);
        }
      } 

      headerElement.prop('id', elementId);

      return elementId;
    }

    function _getPageLink(element)
    {
      var elementId = _setHeaderElementId(element);

      var relativeUrl = new URL(window.location.href);
      relativeUrl.hash = '#' + elementId;

      var linkText = jQuery(element).prop('data-title');
      if (typeof linkText === 'undefined')
      {
        linkText = jQuery(element).text();
      }

      return jQuery('<li><a href="' + relativeUrl.toString() + '">' + linkText + '</a></li>');
    }

    function _isHeaderValid(element)
    {
      let headerArray = options.includeHeaders.split(',');
      let headerIndex = parseInt(element.nodeName.substring(1), 10);
      let includeHeader = headerArray.filter((headers) => headers.toLowerCase().includes('h' + headerIndex)).length > 0;

      if (includeHeader)
      {
        if (options.headingClass.length === 0) return true;

        return jQuery(element).hasClass(options.headingClass);
      }
      else
      {
        return false;
      }
    }

    return this.each(function ()
    {
      if (options.operationMode === 'Automatic' && options.rootSelector.length > 0)
      {
        let listElement = _getAllHeaders(jQuery(options.rootSelector));
        jQuery('.PageLinks').append(listElement);
      }
    });
  };
}(jQuery));