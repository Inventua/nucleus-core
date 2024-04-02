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
      includeHeaders: ''
    }, conf);

    var autoIdCounter = 0;


    // private functions
    function _addLevel(previousHeaderElement, pageLinkParentElement, headerElements, previousLevel, currentHeaderLevel)
    {
      var nextLevel = currentHeaderLevel + 1;
      // Get headers that have been enabled in the settings.
      //var headerArray = options.includeHeaders.split(',');
      //var includeHeader = headerArray.filter((headers) => headers.toLowerCase().includes('h' + currentHeaderLevel)).length > 0;

      jQuery.each(headerElements, function (index, element)
      {
        var headerElement = jQuery(element);

        //if (includeHeader && (currentHeaderLevel === 1 || headerElement.prevAll('h' + previousLevel).first().is(previousHeaderElement)))
        //if (includeHeader) 
        //{
        autoIdCounter++;
        var elementId = headerElement.attr('id');

        if (typeof elementId === 'undefined')
        {
          elementId = 'pageLink' + autoIdCounter;
          headerElement.attr('id', elementId);
        }

        var relativeUrl = new URL(window.location.href);
        relativeUrl.hash = '#' + elementId;

        var linkText = linkText = headerElement.attr('data-title');
        if (typeof linkText === 'undefined')
        {
          linkText = headerElement.text();
        }

        if (linkText !== '')
        {
          //var linkElement = jQuery('<li><a href="' + relativeUrl.toString() + '">' + linkText + '</a></li>');

          var linkElement;
          linkElement = jQuery('<li><a href="' + relativeUrl.toString() + '">' + linkText + '</a></li>');
          //if (includeHeader.length > 0)
          //{
          //  linkElement = jQuery('<li><a href="' + relativeUrl.toString() + '">' + linkText + '</a></li>');
          //}
          //else
          //{
          //  linkElement = jQuery('<li></li>');
          //}

          if (currentHeaderLevel < 6)
          {
            _addPageLink(headerElement, linkElement, currentHeaderLevel, nextLevel);
          }
          pageLinkParentElement.append(linkElement);

        }
        //}
      });

      // Handle cases where the content has skipped a header level (for example H2 ... H4)
      if (headerElements.length === 0 && currentHeaderLevel < 6)
      {
        var linkElement = jQuery('<li></li>');
        pageLinkParentElement.append(linkElement);
        _addPageLink(previousHeaderElement, linkElement, previousLevel, nextLevel);
      }
    }

    function _getChildren(element, elementLevel, nextLevel)
    {
      var selector = 'h' + elementLevel + ',' + 'h' + nextLevel;
      var childElements = element.parent().find(selector);
      var results = [];
      var thisElementFound = false;
      for (var count = 0; count < childElements.length; count++) 
      {
        var item = jQuery(childElements[count]);

        if (item.is(element))
        {
          thisElementFound = true;
        }
        else if (thisElementFound)
        {
          if (item.is('h' + elementLevel)) break;
          results[results.length] = item;
        }
      }

      return results;
    }

    function _addPageLink(headerElement, linkElement, headerLevel, nextLevel)
    {
      var selectedHeaderArray = options.includeHeaders.split(',');
      var doIncludeHeader = selectedHeaderArray.filter((headers) => headers.toLowerCase().includes('h' + nextLevel)).length > 0;

      var pageLinkParentElement = jQuery('<ol></ol>');
      var childElements;

      if (doIncludeHeader)
      {
        childElements = _getChildren(headerElement, headerLevel, nextLevel);// headerElement.siblings('h' + nextLevel);
      }
      else
      {
        childElements = [];
      }

      _addLevel(headerElement, pageLinkParentElement, childElements, headerLevel, nextLevel);

      if (pageLinkParentElement.children().length !== 0)
      {
        //if (headerElement.children().length === 0)
        //{
        //  var wrapperListItem = jQuery('<li></li>');
        //  wrapperListItem.append(pageLinkParentElement);
        //  linkElement.append(wrapperListItem);
        //}
        //else
        //{
          linkElement.append(pageLinkParentElement);
        //}
      }
    }



    return this.each(function ()
    {
      var listElement = jQuery('<ol></ol>');

      if (options.operationMode === 'Automatic' && options.rootSelector.length > 0)
      {
        var childElements = jQuery(options.rootSelector).find('h1');

        //if (childElements.length !== 0)
        //{
        _addLevel(options.rootSelector, listElement, childElements, -1, 1);
        jQuery('.PageLinks').append(listElement);
        //}
      }
    });
  };
}(jQuery));