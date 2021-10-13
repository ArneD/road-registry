import { Elm } from './output/download-product.js'

Elm.DownloadProduct.init({
	node: document.getElementById('app'),
	flags: {
		endpoint: window.wegenregisterApiOldEndpoint,
		apikey: window.wegenregisterApiKey
	}
});
