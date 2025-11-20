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

						// Toast/Alert göster
						showToastAlert('success', data.message || 'Ürün sepete eklendi!');
					} catch (e) { console.warn(e); }
				} else if (data && data.redirect) {
					// Giriş yapılmamış - Modal göster
					showLoginAlert(data.message || 'Ürün sepetine eklemek için giriş yapmanız gerekir.', data.redirect);
				} else {
					showToastAlert('error', (data && data.message) || 'Sepete eklenirken bir hata oluştu.');
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

// Toast Alert Fonksiyonu
function showToastAlert(type, message) {
	var toastContainer = document.getElementById('toast-container');
	if (!toastContainer) {
		toastContainer = document.createElement('div');
		toastContainer.id = 'toast-container';
		toastContainer.style.cssText = 'position: fixed; top: 20px; right: 20px; z-index: 9999;';
		document.body.appendChild(toastContainer);
	}

	var toastHtml = document.createElement('div');
	var bgColor = type === 'success' ? 'bg-success' : type === 'error' ? 'bg-danger' : 'bg-warning';
	
	toastHtml.innerHTML = `
		<div class="alert ${bgColor} text-white alert-dismissible fade show rounded-3 shadow-lg" role="alert" style="min-width: 300px; animation: slideInRight 0.3s ease;">
			<i class="bi bi-${type === 'success' ? 'check-circle' : type === 'error' ? 'exclamation-circle' : 'info-circle'} me-2"></i>
			${message}
			<button type="button" class="btn-close btn-close-white" data-bs-dismiss="alert"></button>
		</div>
	`;

	toastContainer.appendChild(toastHtml);
	
	// Auto remove after 3 seconds
	setTimeout(function() {
		toastHtml.remove();
	}, 3000);
}

// Login Modal Fonksiyonu
function showLoginAlert(message, redirectUrl) {
	var modalHtml = `
		<div class="modal fade" id="loginAlertModal" tabindex="-1">
			<div class="modal-dialog modal-dialog-centered">
				<div class="modal-content border-0 shadow-lg">
					<div class="modal-header bg-primary text-white border-0">
						<h5 class="modal-title">
							<i class="bi bi-exclamation-triangle me-2"></i>Giriş Gerekli
						</h5>
						<button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
					</div>
					<div class="modal-body p-4">
						<p class="mb-0">${message}</p>
						<div class="mt-4 p-3 bg-light rounded-3">
							<small class="text-muted">
								<i class="bi bi-shield-check me-1"></i>
								Giriş yaparak sepete ürün ekleyebilir, siparişlerinizi takip edebilir ve daha pek çok avantajdan yararlanabilirsiniz.
							</small>
						</div>
					</div>
					<div class="modal-footer border-0 gap-2">
						<button type="button" class="btn btn-outline-secondary rounded-3" data-bs-dismiss="modal">
							Daha Sonra
						</button>
						<a href="${redirectUrl}" class="btn btn-primary rounded-3">
							<i class="bi bi-box-arrow-in-right me-2"></i>Giriş Yap
						</a>
					</div>
				</div>
			</div>
		</div>
	`;

	// Eğer modal zaten var ise kaldır
	var existing = document.getElementById('loginAlertModal');
	if (existing) existing.remove();

	// Modal ekle ve göster
	document.body.insertAdjacentHTML('beforeend', modalHtml);
	var modal = new bootstrap.Modal(document.getElementById('loginAlertModal'));
	modal.show();
}

// CSS Animation Stili Ekle
var style = document.createElement('style');
style.textContent = `
	@keyframes slideInRight {
		from {
			transform: translateX(400px);
			opacity: 0;
		}
		to {
			transform: translateX(0);
			opacity: 1;
		}
	}
	
	.hover-lift {
		transition: all 0.3s ease;
	}
	
	.hover-lift:hover {
		transform: translateY(-8px);
		box-shadow: 0 12px 24px rgba(0, 0, 0, 0.15) !important;
	}
	
	.transition-all {
		transition: all 0.3s ease;
	}

	.heart-btn {
		transition: all 0.2s ease;
	}

	.heart-btn:hover {
		background-color: #fff3cd !important;
		color: #dc2626;
	}

	.heart-btn.liked {
		background-color: #fee2e2 !important;
		color: #dc2626;
	}

	.pulse {
		animation: pulse 0.6s ease;
	}

	@keyframes pulse {
		0%, 100% { transform: scale(1); }
		50% { transform: scale(1.2); }
	}
`;
document.head.appendChild(style);

