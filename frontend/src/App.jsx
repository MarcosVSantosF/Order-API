import { useEffect, useState } from "react";

function App() {
  const [orders, setOrders] = useState([]);
  const [cliente, setCliente] = useState("");
  const [produto, setProduto] = useState("");
  const [valor, setValor] = useState("");

  const carregarPedidos = async () => {
    try {
      const response = await fetch("http://localhost:5006/Order");

      if (!response.ok) {
        throw new Error("Erro ao buscar pedidos");
      }

      const data = await response.json();
      setOrders(data);
    } catch (error) {
      console.error(error);
    }
  };

  useEffect(() => {
    carregarPedidos();
  }, []);

  const criarPedido = async (event) => {
    event.preventDefault();

    try {
      const response = await fetch("http://localhost:5006/Order", {
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
    } catch (error) {
      console.error(error);
    }
  };

  return (
    <div className="min-h-screen bg-gray-100 p-8">
      <div className="mx-auto max-w-6xl">

        <h1 className="mb-8 text-3xl font-bold text-gray-800">
          Gestão de Pedidos
        </h1>

        <div className="grid gap-8 md:grid-cols-3">

          {/* Formulário */}
          <div className="rounded-lg bg-white p-6 shadow md:col-span-1">
            <h2 className="mb-4 text-xl font-semibold">
              Novo Pedido
            </h2>

            <form onSubmit={criarPedido} className="space-y-4">

              <div>
                <label className="mb-1 block text-sm font-medium">
                  Cliente
                </label>

                <input
                  type="text"
                  value={cliente}
                  onChange={(e) => setCliente(e.target.value)}
                  className="w-full rounded border p-2"
                  required
                />
              </div>

              <div>
                <label className="mb-1 block text-sm font-medium">
                  Produto
                </label>

                <input
                  type="text"
                  value={produto}
                  onChange={(e) => setProduto(e.target.value)}
                  className="w-full rounded border p-2"
                  required
                />
              </div>

              <div>
                <label className="mb-1 block text-sm font-medium">
                  Valor
                </label>

                <input
                  type="number"
                  step="0.01"
                  value={valor}
                  onChange={(e) => setValor(e.target.value)}
                  className="w-full rounded border p-2"
                  required
                />
              </div>

              <button
                type="submit"
                className="w-full rounded bg-blue-600 px-4 py-2 font-semibold text-white hover:bg-blue-700"
              >
                Criar Pedido
              </button>

            </form>
          </div>

          {/* Lista */}
          <div className="rounded-lg bg-white p-6 shadow md:col-span-2">

            <div className="mb-4 flex items-center justify-between">
              <h2 className="text-xl font-semibold">
                Pedidos
              </h2>

              <button
                onClick={carregarPedidos}
                className="rounded bg-gray-200 px-3 py-2 text-sm hover:bg-gray-300"
              >
                Atualizar
              </button>
            </div>

            <div className="space-y-3">

              {orders.length === 0 ? (
                <p className="text-gray-500">
                  Nenhum pedido encontrado.
                </p>
              ) : (
                orders.map((order) => (
                  <div
                    key={order.id}
                    className="rounded border p-4"
                  >
                    <div className="flex justify-between">

                      <div>
                        <p className="font-semibold">
                          Pedido #{order.id}
                        </p>

                        <p className="text-sm text-gray-600">
                          {order.cliente}
                        </p>

                        <p className="text-sm text-gray-600">
                          {order.produto}
                        </p>
                      </div>

                      <div className="text-right">
                        <p className="font-semibold">
                          R$ {Number(order.valor).toFixed(2)}
                        </p>

                        <span className="text-sm text-blue-600">
                          {order.status}
                        </span>
                      </div>

                    </div>
                  </div>
                ))
              )}

            </div>

          </div>

        </div>
      </div>
    </div>
  );
}

export default App;