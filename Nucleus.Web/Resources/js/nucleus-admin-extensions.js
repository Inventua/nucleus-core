var ExtensionsStore = new _extensionsStore();

function _extensionsStore()
{
  this.SetUrls = _setUrls;
  _queryUrl = '';
  _installUrl = '';

  window.addEventListener('message', _handleStoreEvents, false);

  function _setUrls(queryUrl, installUrl)
  {
    _queryUrl = queryUrl;
    _installUrl = installUrl;
  }

  function _handleStoreEvents(event)
  {
    var storeWindow = jQuery('#StoreFrame')[0].contentWindow;
    switch (event.data.method)
    {
      case 'install':
        var newEvent = jQuery.Event('submit', { originalEvent: event });
        jQuery('#' + 'nucleus-extensions-store-form')
          .attr('action', _installUrl + '?id=' + event.data.id);
        jQuery('#' + 'nucleus-extensions-store-form').trigger(newEvent);
        break;

      case 'query':
        jQuery.ajax({
          url: _queryUrl,
          method: 'GET',
          data: { id: event.data.id },
          headers: { 'Accept': 'application/json, */*' },
          success: function (data, status, request)
          {
            data.type = 'query';
            storeWindow.postMessage(data, '*');
          },
          error: function (request, status, message)
          {
            // no error handling
          }
        })

      //var result = new Object();

      //result.type = 'query';
      //result.IsInstalled = true;
      //result.Version = '1.0.0.0';

      //jQuery('#StoreFrame')[0].contentWindow.postMessage(result, '*');

      case 'detect':
        var result = new Object();

        result.type = 'detect';
        storeWindow.postMessage(result, '*');
    }
  }
}