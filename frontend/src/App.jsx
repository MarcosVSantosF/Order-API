import { useEffect, useMemo, useState } from "react";

const API_URL = import.meta.env.VITE_API_URL;

const STATUS_CONFIG = {
  Pendente: { label: "Pendente", dot: "bg-amber-500", text: "text-amber-800", bg: "bg-amber-50", ring: "ring-amber-200" },
  Processando: { label: "Processando", dot: "bg-blue-500", text: "text-blue-800", bg: "bg-blue-50", ring: "ring-blue-200" },
  Finalizado: { label: "Finalizado", dot: "bg-emerald-500", text: "text-emerald-800", bg: "bg-emerald-50", ring: "ring-emerald-200" },
};

const TOAST_CONFIG = {
  success: { dot: "bg-emerald-500", border: "border-l-emerald-500" },
  error: { dot: "bg-red-500", border: "border-l-red-500" },
};

function StatusBadge({ status }) {
  const config = STATUS_CONFIG[status] ?? {
    label: status,
    dot: "bg-stone-400",
    text: "text-stone-700",
    bg: "bg-stone-50",
    ring: "ring-stone-200",
  };

  return (
    <span className={`inline-flex items-center gap-1.5 rounded px-2 py-0.5 text-xs font-medium ring-1 ring-inset ${config.bg} ${config.text} ${config.ring}`}>
      <span className={`h-1.5 w-1.5 rounded-full ${config.dot}`} />
      {config.label}
    </span>
  );
}

function ToastContainer({ toasts }) {
  return (
    <div className="fixed bottom-6 right-6 z-50 flex flex-col gap-2">
      {toasts.map((toast) => {
        const config = TOAST_CONFIG[toast.type] ?? TOAST_CONFIG.success;
        return (
          <div
            key={toast.id}
            className={`flex items-center gap-2 rounded border border-stone-200 border-l-4 bg-white px-4 py-3 text-sm shadow-sm ${config.border}`}
            style={{ animation: "toast-in 0.2s ease-out" }}
          >
            <span className={`h-1.5 w-1.5 rounded-full ${config.dot}`} />
            {toast.message}
          </div>
        );
      })}
    </div>
  );
}

function App() {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [creating, setCreating] = useState(false);
  const [toasts, setToasts] = useState([]);

  const [cliente, setCliente] = useState("");
  const [produto, setProduto] = useState("");
  const [valor, setValor] = useState("");
  const [statusFiltro, setStatusFiltro] = useState("Todos");

  const addToast = (message, type = "success") => {
    const id = crypto.randomUUID();
    setToasts((prev) => [...prev, { id, message, type }]);

    setTimeout(() => {
      setToasts((prev) => prev.filter((t) => t.id !== id));
    }, 3000);
  };

  const carregarPedidos = async ({ showFeedback = false, status = statusFiltro } = {}) => {
  if (showFeedback) setRefreshing(true);

  try {
    const url = status && status !== "Todos"
      ? `${API_URL}/Order?status=${status}`
      : `${API_URL}/Order`;

    const response = await fetch(url);

    if (!response.ok) {
      throw new Error("Erro ao buscar pedidos");
    }

    const data = await response.json();
    setOrders(data);

    if (showFeedback) addToast("Lista atualizada");
  } catch (error) {
    console.error(error);
    if (showFeedback) addToast("Não foi possível atualizar a lista", "error");
  } finally {
    setLoading(false);
    if (showFeedback) setRefreshing(false);
  }
};

const aplicarFiltro = (status) => {
  setStatusFiltro(status);
  carregarPedidos({ status });
};

  useEffect(() => {
    carregarPedidos();
  }, []);

  const criarPedido = async (event) => {
    event.preventDefault();
    setCreating(true);

    try {
      const response = await fetch(`${API_URL}/Order`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          cliente,
          produto,
          valor: Number(valor),
        }),
      });

      if (!response.ok) {
        throw new Error("Erro ao criar pedido");
      }

      setCliente("");
      setProduto("");
      setValor("");

      await carregarPedidos();
      addToast("Pedido criado com sucesso");
    } catch (error) {
      console.error(error);
      addToast("Não foi possível criar o pedido", "error");
    } finally {
      setCreating(false);
    }
  };

  const resumo = useMemo(() => {
    return orders.reduce(
      (acc, order) => {
        acc[order.status] = (acc[order.status] ?? 0) + 1;
        return acc;
      },
      { Pendente: 0, Processando: 0, Finalizado: 0 }
    );
  }, [orders]);

  return (
    <div className="min-h-screen bg-[#F7F7F5] text-[#1C1F26]" style={{ fontFamily: "'IBM Plex Sans', sans-serif" }}>
      <div className="mx-auto max-w-6xl px-6 py-10">

        <div className="mb-8 flex flex-wrap items-end justify-between gap-4 border-b border-stone-200 pb-6">
          <div>
            <h1 className="text-2xl font-semibold tracking-tight">Gestão de Pedidos</h1>
            <p className="mt-1 text-sm text-stone-500">Acompanhamento de pedidos em tempo real</p>
          </div>

          <div className="flex gap-6">
            {Object.entries(resumo).map(([status, count]) => (
              <div key={status} className="text-right">
                <p className="text-2xl font-semibold tabular-nums" style={{ fontFamily: "'IBM Plex Mono', monospace" }}>
                  {count}
                </p>
                <p className="text-xs text-stone-500">{status}</p>
              </div>
            ))}
          </div>
        </div>

        <div className="grid gap-8 lg:grid-cols-[320px_1fr]">

          <div className="h-fit rounded border border-stone-200 bg-white">
            <div className="border-b border-stone-200 px-5 py-3">
              <h2 className="text-sm font-semibold text-stone-700">Novo pedido</h2>
            </div>

            <form onSubmit={criarPedido} className="space-y-4 px-5 py-5">
              <div>
                <label className="mb-1.5 block text-xs font-medium text-stone-500">
                  Cliente
                </label>
                <input
                  type="text"
                  value={cliente}
                  onChange={(e) => setCliente(e.target.value)}
                  className="w-full rounded border border-stone-300 px-3 py-2 text-sm focus:border-[#1F6F78] focus:outline-none focus:ring-1 focus:ring-[#1F6F78]"
                  required
                />
              </div>

              <div>
                <label className="mb-1.5 block text-xs font-medium text-stone-500">
                  Produto
                </label>
                <input
                  type="text"
                  value={produto}
                  onChange={(e) => setProduto(e.target.value)}
                  className="w-full rounded border border-stone-300 px-3 py-2 text-sm focus:border-[#1F6F78] focus:outline-none focus:ring-1 focus:ring-[#1F6F78]"
                  required
                />
              </div>

              <div>
                <label className="mb-1.5 block text-xs font-medium text-stone-500">
                  Valor
                </label>
                <div className="flex items-center rounded border border-stone-300 focus-within:border-[#1F6F78] focus-within:ring-1 focus-within:ring-[#1F6F78]">
                  <span className="pl-3 text-sm text-stone-400">R$</span>
                  <input
                    type="number"
                    step="0.01"
                    value={valor}
                    onChange={(e) => setValor(e.target.value)}
                    className="w-full bg-transparent px-2 py-2 text-sm focus:outline-none"
                    required
                  />
                </div>
              </div>

              <button
                type="submit"
                disabled={creating}
                className="w-full rounded bg-[#1F6F78] px-4 py-2.5 text-sm font-medium text-white transition-colors hover:bg-[#19585F] disabled:cursor-not-allowed disabled:opacity-60"
              >
                {creating ? "Criando..." : "Criar pedido"}
              </button>
            </form>
          </div>

          <div className="rounded border border-stone-200 bg-white">
            <div className="flex flex-wrap items-center justify-between gap-3 border-b border-stone-200 px-5 py-3">
  <h2 className="text-sm font-semibold text-stone-700">Pedidos</h2>

  <div className="flex items-center gap-4">
    <div className="flex gap-1.5">
      {["Todos", "Pendente", "Processando", "Finalizado"].map((status) => (
        <button
          key={status}
          onClick={() => aplicarFiltro(status)}
          className={`rounded px-2.5 py-1 text-xs font-medium transition-colors ${
            statusFiltro === status
              ? "bg-[#1F6F78] text-white"
              : "bg-stone-100 text-stone-600 hover:bg-stone-200"
          }`}
        >
          {status}
        </button>
      ))}
    </div>

    <button
      onClick={() => carregarPedidos({ showFeedback: true })}
      disabled={refreshing}
      className="text-xs font-medium text-stone-500 hover:text-[#1F6F78] disabled:cursor-not-allowed disabled:opacity-60"
    >
      {refreshing ? "Atualizando..." : "Atualizar"}
    </button>
  </div>
</div>

            {loading ? (
              <p className="px-5 py-8 text-center text-sm text-stone-400">Carregando pedidos...</p>
            ) : orders.length === 0 ? (
              <p className="px-5 py-8 text-center text-sm text-stone-400">
                Nenhum pedido registrado ainda. Crie o primeiro ao lado.
              </p>
            ) : (
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-stone-200 text-left text-xs text-stone-500">
                    <th className="px-5 py-2 font-medium">ID</th>
                    <th className="px-5 py-2 font-medium">Cliente</th>
                    <th className="px-5 py-2 font-medium">Produto</th>
                    <th className="px-5 py-2 font-medium text-right">Valor</th>
                    <th className="px-5 py-2 font-medium">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {orders.map((order) => (
                    <tr key={order.id} className="border-b border-stone-100 last:border-0">
                      <td className="px-5 py-3 text-stone-400" style={{ fontFamily: "'IBM Plex Mono', monospace" }}>
                        #{order.id}
                      </td>
                      <td className="px-5 py-3 font-medium">{order.cliente}</td>
                      <td className="px-5 py-3 text-stone-600">{order.produto}</td>
                      <td className="px-5 py-3 text-right tabular-nums" style={{ fontFamily: "'IBM Plex Mono', monospace" }}>
                        R$ {Number(order.valor).toFixed(2)}
                      </td>
                      <td className="px-5 py-3">
                        <StatusBadge status={order.status} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>

        </div>
      </div>

      <ToastContainer toasts={toasts} />
    </div>
  );
}

export default App;