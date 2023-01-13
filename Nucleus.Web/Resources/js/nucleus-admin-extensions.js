var ExtensionsStore = new _extensionsStore();

function _extensionsStore()
{
  this.SetUrls = _setUrls;
  _queryUrl = '';
  _installUrl = '';
  _queryStoreUrl = '';

  window.addEventListener('message', _handleStoreEvents, false);

  function _setUrls(queryUrl, installUrl, queryStoreUrl)
  {
    _queryUrl = queryUrl;
    _installUrl = installUrl;
    _queryStoreUrl = queryStoreUrl;
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
          async: true,
          method: 'GET',
          data: { id: event.data.id },
          headers: { 'Accept': 'application/json, */*' },
          success: function (data, status, request)
          {
            data.type = 'query';
            storeWindow.postMessage(data, '*');
          }
        })

      case 'detect':
        var form = jQuery('#nucleus-extensions-store-form');
        jQuery.ajax({
          url: _queryStoreUrl,
          async: true,
          method: 'POST',
          enctype: form.attr('enctype'),
          data: (form.attr('enctype') === 'multipart/form-data') ? new FormData(form[0]) : form.serialize(),
          processData: (form.attr('enctype') === 'multipart/form-data') ? false : true,
          contentType: (form.attr('enctype') === 'multipart/form-data') ? false : 'application/x-www-form-urlencoded; charset=UTF-8',
          headers: { 'Accept': 'application/json, */*' },
          success: function (data, status, request)
          {
            data.type = 'detect';
            storeWindow.postMessage(data, '*');
          }
        })

    }
  }
}