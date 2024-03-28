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
      var headerArray = options.includeHeaders.split(',');
      var includeHeader = headerArray.filter((headers) => headers.toLowerCase().includes('h' + currentHeaderLevel)); 

      headerElements.each(function (index, element)
      {
        var headerElement = jQuery(element);

        if (currentHeaderLevel === 1 || headerElement.prevAll('h' + previousLevel).first().is(previousHeaderElement))
        {
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
            if (includeHeader.length > 0)
            {
              linkElement = jQuery('<li><a href="' + relativeUrl.toString() + '">' + linkText + '</a></li>');
            }
            else
            {
              linkElement = jQuery('<li></li>');
            }

            if (currentHeaderLevel < 6)
            {
              _addPageLink(headerElement, linkElement, currentHeaderLevel, nextLevel);
            }
            pageLinkParentElement.append(linkElement);

          }
        }
      });

      // Handle cases where the content has skipped a header level (for example H2 ... H4)
      if (headerElements.length === 0 && currentHeaderLevel < 6)
      {
        _addPageLink(previousHeaderElement, pageLinkParentElement, previousLevel, nextLevel);
      }
    }

    function _addPageLink(headerElement, linkElement, headerLevel, nextLevel)
    {
      var pageLinkParentElement = jQuery('<ol></ol>');
      var childElements = headerElement.siblings('h' + nextLevel);
    //  var headerArray = options.includeHeaders.split(',');
    //  var includeHeader = headerArray.filter((headers) => headers.toLowerCase().includes('h' + headerLevel)); 

      _addLevel(headerElement, pageLinkParentElement, childElements, headerLevel, nextLevel);

      if (pageLinkParentElement.children().length !== 0)
      {
        linkElement.append(pageLinkParentElement);
      }
    }


    return this.each(function ()
    {
      var listElement = jQuery('<ol></ol>');
     
      if (options.operationMode === 'Automatic' && options.rootSelector.length > 0)
      {
        var childElements = jQuery(options.rootSelector).find('h1');

        if (childElements.length !== 0)
        {
          _addLevel(options.rootSelector, listElement, childElements, -1, 1);
          jQuery('.PageLinks').append(listElement);
        }
      }
    });
  };
}(jQuery));