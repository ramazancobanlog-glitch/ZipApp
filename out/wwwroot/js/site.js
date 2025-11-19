// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener('DOMContentLoaded', function () {
	// intercept forms with class ajax-add-to-cart and submit via fetch
	document.querySelectorAll('form.ajax-add-to-cart').forEach(function (form) {
		form.addEventListener('submit', function (e) {
			e.preventDefault();

			var formData = new FormData(form);

			// include antiforgery token header if present
			var headers = { 'X-Requested-With': 'XMLHttpRequest' };
			var tokenMeta = document.querySelector('meta[name="csrf-token"]');
			if (tokenMeta) headers['RequestVerificationToken'] = tokenMeta.getAttribute('content');

			fetch(form.action, {
				method: 'POST',
				body: formData,
				headers: headers
			})
			.then(function (res) { return res.json(); })
			.then(function (data) {
				if (data && data.success) {				
					// update badge count and animate 1-2-3 near the cart icon
					try {
						var badge = document.getElementById('cart-badge');
						if (badge) {
							var current = parseInt(badge.textContent || '0', 10) || 0;
							var newCount = data.count ?? (current + 1);
							badge.textContent = newCount;
							// small pulse
							badge.classList.add('pulse');
							setTimeout(function(){ badge.classList.remove('pulse'); }, 900);
						}

						// 1-2-3 animation (ana sepet ikonu)
						var fly = document.getElementById('cart-fly');
						if (fly) {
							fly.textContent = '1';
							fly.classList.add('show');
							setTimeout(function(){ fly.textContent = '2'; }, 300);
							setTimeout(function(){ fly.textContent = '3'; }, 600);
							setTimeout(function(){ fly.classList.remove('show'); fly.textContent = ''; }, 1100);
						}

						// Sepete Ekle butonunda animasyon
						var btn = form.querySelector('button[type="submit"]');
						var flyBtn = btn ? btn.querySelector('.cart-fly-btn') : null;
						if (flyBtn) {
							flyBtn.textContent = '1';
							flyBtn.classList.add('show');
							setTimeout(function(){ flyBtn.textContent = '2'; }, 300);
							setTimeout(function(){ flyBtn.textContent = '3'; }, 600);
							setTimeout(function(){ flyBtn.classList.remove('show'); flyBtn.textContent = ''; }, 1100);
						}
					} catch (e) { console.warn(e); }
				} else if (data && data.redirect) {
					window.location = data.redirect;
				} else {
					alert((data && data.message) || 'Sepete eklenirken bir hata oluştu.');
				}
			})
			.catch(function (err) {
				console.error(err);
				alert('Sepete ekleme isteği başarısız.');
			});
		});
	});

	// Category filtering
	var chips = document.querySelectorAll('.category-chip');
	if (chips.length) {
		chips.forEach(function(chip){
			chip.addEventListener('click', function(){
				chips.forEach(c => c.classList.remove('active'));
				chip.classList.add('active');
				var id = chip.getAttribute('data-id');
				var cards = document.querySelectorAll('.product-card');
				if (id === '0') { // show all
					cards.forEach(function(card){ card.style.display = ''; });
				} else {
					cards.forEach(function(card){
						var cat = card.getAttribute('data-category-id');
						if (cat === id) card.style.display = ''; else card.style.display = 'none';
					});
				}
			});
		});
	}


	// Sepetteki ürünü silme
	document.querySelectorAll('.remove-item-btn').forEach(function(btn){
		btn.addEventListener('click', function(){
			var itemId = btn.getAttribute('data-id');
			var row = btn.closest('tr');
			var tokenMeta = document.querySelector('meta[name="csrf-token"]');
			var headers = { 'X-Requested-With': 'XMLHttpRequest' };
			if (tokenMeta) headers['RequestVerificationToken'] = tokenMeta.getAttribute('content');
			fetch('/Cart/RemoveItem', {
				method: 'POST',
				headers: headers,
				body: new URLSearchParams({ itemId: itemId })
			})
			.then(function(res){ return res.json(); })
			.then(function(data){
				if(data && data.success){
					if(row) row.remove();
				}else if(data && data.redirect){
					window.location = data.redirect;
				}else{
					alert((data && data.message) || 'Ürün silinemedi.');
				}
			})
			.catch(function(){ alert('Silme işlemi başarısız.'); });
		});
	});
});
