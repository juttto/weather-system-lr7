// Клієнтська частина: звертається до REST API через fetch (запити з іншого домену дозволяє CORS)
const API = window.API_BASE_URL.replace(/\/$/, "");
const $ = (id) => document.getElementById(id);
let devices = [];

const typeText = (t) => (t === "Actuator" ? "Виконавчий пристрій" : "Датчик");
const num = (v, unit) => (v === null || v === undefined ? "—" : `${Number(v).toLocaleString("uk-UA", { maximumFractionDigits: 1 })}${unit}`);

function escapeHtml(text) {
  const div = document.createElement("div");
  div.textContent = text;
  return div.innerHTML;
}

// GET /health/ready — стан API і бази даних
async function checkHealth() {
  const box = $("status");
  try {
    const res = await fetch(`${API}/health/ready`);
    const data = await res.json();
    const ok = data.status === "Healthy";
    box.className = "status " + (ok ? "ok" : "bad");
    $("statusText").textContent = ok ? "API і база даних: Healthy" : `API: ${data.status}`;
  } catch {
    box.className = "status bad";
    $("statusText").textContent = "API недоступний";
  }
}

// GET /api/devices?name=...
async function loadDevices() {
  const name = $("search").value.trim();
  $("info").textContent = "Завантаження… (безкоштовний сервер Render може прокидатися до хвилини)";
  try {
    const url = name ? `${API}/api/devices?name=${encodeURIComponent(name)}` : `${API}/api/devices`;
    const res = await fetch(url);
    devices = await res.json();
    $("info").textContent = `Отримано з API: ${devices.length} пристр. (GET ${url.replace(API, "")})`;
  } catch {
    devices = [];
    $("info").textContent = "Не вдалося отримати дані з API.";
  }
  render();
  checkHealth();
}

function render() {
  const grid = $("grid");
  grid.innerHTML = devices.length ? "" : '<p class="hint">Пристроїв немає</p>';
  for (const d of devices) {
    const card = document.createElement("article");
    card.className = "card" + (d.isEnabled ? " on" : "");
    card.innerHTML = `
      <h3>${escapeHtml(d.name)}</h3>
      <div class="tags"><span class="tag">${typeText(d.type)}</span><span class="tag state">${d.isEnabled ? "Увімкнений" : "Вимкнений"}</span></div>
      <div class="readings"><span><b>${num(d.temperature, "°")}</b> темп.</span><span><b>${num(d.humidity, "%")}</b> волог.</span></div>
      <div class="meta">Оновлено: ${new Date(d.lastUpdated).toLocaleString("uk-UA")}</div>
      <div class="actions">
        <button class="btn small ghost" data-act="toggle">${d.isEnabled ? "Вимкнути" : "Увімкнути"}</button>
        <button class="btn small danger" data-act="delete">Видалити</button>
      </div>`;
    card.querySelector('[data-act="toggle"]').onclick = () => setStatus(d.id, !d.isEnabled);
    card.querySelector('[data-act="delete"]').onclick = () => removeDevice(d.id, d.name);
    grid.appendChild(card);
  }
  // Статистика по всіх отриманих пристроях
  const avg = (key) => {
    const values = devices.map((d) => d[key]).filter((v) => v !== null && v !== undefined);
    return values.length ? values.reduce((a, b) => a + Number(b), 0) / values.length : null;
  };
  $("sTotal").textContent = devices.length;
  $("sOn").textContent = devices.filter((d) => d.isEnabled).length;
  $("sTemp").textContent = num(avg("temperature"), " °C");
  $("sHum").textContent = num(avg("humidity"), " %");
}

// PATCH /api/devices/{id}/status
async function setStatus(id, isActive) {
  await fetch(`${API}/api/devices/${id}/status`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ isActive }),
  });
  loadDevices();
}

// DELETE /api/devices/{id}
async function removeDevice(id, name) {
  if (!window.confirm(`Видалити «${name}»?`)) return;
  await fetch(`${API}/api/devices/${id}`, { method: "DELETE" });
  loadDevices();
}

// POST /api/devices — помилки 400 від валідатора показуємо під формою
async function addDevice(event) {
  event.preventDefault();
  $("formError").textContent = "";
  const toNumber = (text) => (text.trim() === "" ? null : Number(text.replace(",", ".")));
  const body = {
    name: $("fName").value,
    type: $("fType").value,
    temperature: toNumber($("fTemp").value),
    humidity: toNumber($("fHum").value),
  };
  if (Number.isNaN(body.temperature) || Number.isNaN(body.humidity)) {
    $("formError").textContent = "Температура і вологість мають бути числами (або порожні).";
    return;
  }
  const res = await fetch(`${API}/api/devices`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (res.status === 400) {
    const problem = await res.json();
    $("formError").textContent = Object.values(problem.errors || {}).flat().join(" ");
    return;
  }
  $("form").reset();
  $("search").value = "";
  loadDevices();
}

$("lnkSwagger").href = `${API}/swagger`;
$("lnkHealth").href = `${API}/health`;
$("lnkDb").href = `${API}/api/info/database`;
$("form").addEventListener("submit", addDevice);
$("btnSearch").onclick = loadDevices;
$("btnRefresh").onclick = () => { $("search").value = ""; loadDevices(); };
$("search").addEventListener("keydown", (e) => { if (e.key === "Enter") loadDevices(); });
loadDevices();
